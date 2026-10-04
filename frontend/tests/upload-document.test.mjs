import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import vm from "node:vm";
import ts from "typescript";

// Exercise the real upload flow with HTTP doubles. Hook state and query invalidation do not
// need a DOM; TypeScript is already a development dependency, so no test framework is needed.
const source = readFileSync(new URL("../src/lib/api/useUploadDocument.ts", import.meta.url), "utf8");
const compiled = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText;

function setup(providerBody, providerStatus = 200) {
  const requests = [];
  const providerRequests = [];
  const invalidations = [];
  const ticket = {
    publicId: "documind/owner/upload-id",
    uploadUrl: "https://api.cloudinary.com/v1_1/test/raw/upload",
    apiKey: "test-key",
    timestamp: 123,
    signature: "test-signature",
  };
  const document = { id: "document-id", status: "Uploaded" };
  class ApiError extends Error {
    constructor(status, message, errorCode) {
      super(message);
      this.status = status;
      this.errorCode = errorCode;
    }
  }

  const modules = {
    react: {
      useState: (initial) => [initial, () => {}],
      useRef: (current) => ({ current }),
      useCallback: (callback) => callback,
    },
    "@tanstack/react-query": {
      useQueryClient: () => ({
        invalidateQueries: async (query) => invalidations.push(query),
      }),
    },
    "@/lib/api/client": {
      API_BASE_URL: "http://localhost:5100",
      ApiError,
      getAccessToken: () => "test-token",
      refreshAccessToken: async () => false,
    },
    "@/lib/api/generated/documents/documents": {
      getGetApiDocumentsQueryKey: () => ["/api/Documents"],
    },
  };

  const exports = {};
  vm.runInNewContext(compiled, {
    exports,
    require: (name) => {
      assert.ok(name in modules, `Unexpected import: ${name}`);
      return modules[name];
    },
    FormData,
    fetch: async (url, options) => {
      requests.push({ url, body: JSON.parse(options.body) });
      if (url.endsWith("/upload-ticket")) return Response.json(ticket);
      assert.ok(url.endsWith("/confirm"));
      return Response.json(document);
    },
    XMLHttpRequest: class {
      upload = {};
      open(method, url) {
        this.method = method;
        this.url = url;
      }
      send(form) {
        providerRequests.push({ method: this.method, url: this.url, form });
        this.status = providerStatus;
        this.responseText = providerBody;
        this.onload();
      }
    },
  });

  return { upload: exports.useUploadDocument().upload, requests, providerRequests, invalidations, ticket, document };
}

test("confirms Cloudinary's returned raw public_id when it adds the PDF extension", async () => {
  const uploadedId = "documind/owner/upload-id.pdf";
  const flow = setup(JSON.stringify({ public_id: uploadedId, bytes: 74664 }));
  const file = new File(["%PDF-1.7"], "handbook.pdf", { type: "application/pdf" });

  assert.deepEqual(await flow.upload(file), flow.document);
  assert.equal(flow.providerRequests.length, 1);
  assert.equal(flow.providerRequests[0].url, flow.ticket.uploadUrl);
  // Preserve the signed id on upload, then use the returned id for server-side verification.
  assert.equal(flow.providerRequests[0].form.get("public_id"), flow.ticket.publicId);
  assert.equal(flow.providerRequests[0].form.get("signature"), flow.ticket.signature);
  assert.deepEqual(flow.requests[1].body, { publicId: uploadedId, fileName: "handbook.pdf" });
  assert.equal(flow.invalidations.length, 1);
});

test("also confirms tickets that already contain the extension", async () => {
  const flow = setup(JSON.stringify({ public_id: "documind/owner/upload-id.pdf" }));
  flow.ticket.publicId += ".pdf";

  await flow.upload(new File(["%PDF-1.7"], "handbook.pdf"));

  assert.equal(flow.requests[1].body.publicId, flow.ticket.publicId);
});

for (const body of ["not json", "null", "{}", '{"public_id":42}', '{"public_id":"  "}']) {
  test(`does not confirm an invalid provider response: ${body}`, async () => {
    const flow = setup(body);

    await assert.rejects(flow.upload(new File(["%PDF-1.7"], "handbook.pdf")), {
      errorCode: "INVALID_UPLOAD_RESPONSE",
    });

    assert.equal(flow.requests.length, 1);
    assert.equal(flow.invalidations.length, 0);
  });
}

test("does not confirm a rejected provider upload", async () => {
  const flow = setup('{"error":{"message":"Invalid signature"}}', 401);

  await assert.rejects(flow.upload(new File(["%PDF-1.7"], "handbook.pdf")), {
    errorCode: "PROVIDER_UPLOAD_FAILED",
    message: "Invalid signature",
  });

  assert.equal(flow.requests.length, 1);
});
