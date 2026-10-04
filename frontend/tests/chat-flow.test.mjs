import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import vm from "node:vm";
import ts from "typescript";

function compile(file, extra = "") {
  return ts.transpileModule(readFileSync(new URL(file, import.meta.url), "utf8") + extra, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX },
  }).outputText;
}
const screenCode = compile("../src/app/(app)/chat/page.tsx", "\nexport { ChatScreen };");
const entryCode = compile("../src/lib/chatEntry.ts");

// Run the actual screen with deterministic hook, navigation, query and stream doubles. No
// generated API or network calls are replaced inside the screen itself. Effects are replayed
// with cleanup before microtasks, including the Strict Mode setup/cleanup/setup sequence.
function screen(url, options = {}) {
  let route = new URL(url, "http://localhost");
  let dirty = true;
  let tree;
  let cursor = 0;
  let first = true;
  let docs = options.documents ?? [{ id: "doc", fileName: "Handbook.pdf", status: "Completed" }];
  const slots = [];
  const microtasks = [];
  let effects = [];
  const starts = [], sends = [], navigations = [];
  const user = { id: options.userId ?? crypto.randomUUID() };
  const conversations = options.conversations ?? [
    { id: "old", documentId: "doc", title: "Old", updatedAt: "2026-10-01T00:00:00Z" },
    { id: "latest", documentId: "doc", title: "Latest", updatedAt: "2026-10-04T00:00:00Z" },
  ];
  const threads = new Map(conversations.map((c) => [c.id, { ...c, documentName: "Handbook.pdf", messages: [] }]));
  const changed = (a, b) => !a || !b || a.length !== b.length || a.some((value, i) => !Object.is(value, b[i]));
  const hooks = {
    useState(initial) {
      const i = cursor++;
      slots[i] ??= { value: typeof initial === "function" ? initial() : initial };
      return [slots[i].value, (next) => {
        const value = typeof next === "function" ? next(slots[i].value) : next;
        if (!Object.is(value, slots[i].value)) { slots[i].value = value; dirty = true; }
      }];
    },
    useRef(initial) { const i = cursor++; slots[i] ??= { current: initial }; return slots[i]; },
    useCallback(fn, deps) {
      const i = cursor++;
      if (!slots[i] || changed(slots[i].deps, deps)) slots[i] = { fn, deps };
      return slots[i].fn;
    },
    useEffect(fn, deps) {
      const i = cursor++;
      if (!slots[i] || changed(slots[i].deps, deps)) {
        effects.push({ i, fn, deps });
      }
    },
    Suspense: "Suspense",
  };
  const navigate = (next) => {
    if (route.pathname + route.search === next) return;
    navigations.push(next);
    route = new URL(next, route);
    dirty = true;
  };
  const router = { replace: navigate };
  const queryClient = {
    invalidateQueries: () => { dirty = true; },
    setQueryData: () => {},
  };
  const start = { mutateAsync: async (request) => {
    starts.push(request);
    if (options.create) await options.create();
    const thread = { id: "created", documentId: request.data.documentId, documentName: "Handbook.pdf", title: "Created", messages: [] };
    threads.set(thread.id, thread);
    return thread;
  } };
  const storageMap = options.storageMap ?? new Map();
  const storage = { getItem: (key) => storageMap.get(key), setItem: (key, value) => storageMap.set(key, value) };
  const entry = {};
  vm.runInNewContext(entryCode, { exports: entry, URLSearchParams, window: { sessionStorage: storage } });
  const toast = () => {};
  const modules = {
    react: hooks,
    "react/jsx-runtime": { jsx: (type, props) => ({ type, props }), jsxs: (type, props) => ({ type, props }) },
    "next/link": { default: "Link" },
    "next/navigation": { useSearchParams: () => route.searchParams, useRouter: () => router, usePathname: () => route.pathname },
    "@tanstack/react-query": { useQueryClient: () => queryClient },
    "@/lib/auth/AuthProvider": { useAuth: () => ({ user }) },
    "@/lib/chatEntry": entry,
    "@/lib/api/client": { ApiError: class extends Error {} },
    "@/lib/data": { SUGGESTIONS: [] },
    "@/components/ui/Toast": { useToast: () => toast },
    "@/lib/api/generated/documents/documents": { useGetApiDocuments: () => ({ data: docs, isSuccess: docs !== undefined }) },
    "@/lib/api/generated/conversations/conversations": {
      useGetApiConversations: () => ({ data: conversations, isPending: false }),
      useGetApiConversationsId: (id) => ({ data: threads.get(id), isPending: !id, isError: false }),
      usePostApiConversations: () => start,
      useDeleteApiConversationsId: () => ({}),
      getGetApiConversationsQueryKey: () => ["conversations"],
      getGetApiConversationsIdQueryKey: (id) => ["conversations", id],
    },
    "@/lib/api/stream": { streamAnswer: async (request) => {
      sends.push(request);
      if (options.stream) await options.stream(request, sends.length);
    } },
  };
  const exported = {};
  vm.runInNewContext(screenCode, {
    exports: exported, URLSearchParams, AbortController, DOMException,
    queueMicrotask: (fn) => microtasks.push(fn),
    require: (name) => {
      if (modules[name]) return modules[name];
      if (name.startsWith("@/components/")) return new Proxy({}, { get: (_, key) => key });
      throw new Error(`Unexpected import ${name}`);
    },
  });
  function find(type) {
    const pending = [tree];
    const seen = new Set();
    while (pending.length) {
      const node = pending.pop();
      if (!node || typeof node !== "object" || seen.has(node)) continue;
      seen.add(node);
      if (node.type === type) return node.props;
      const children = node.props?.children;
      if (Array.isArray(children)) pending.push(...children.flat(Infinity));
      else pending.push(children);
    }
  }
  return {
    starts, sends, navigations, storageMap, user,
    get route() { return route; },
    get composer() { return find("ChatComposer"); },
    get pane() { return find("ConversationPane"); },
    navigate,
    setDocuments(next) { docs = next; dirty = true; },
    async flush() {
      for (let pass = 0; pass < 25; pass++) {
        if (dirty) {
          dirty = false; cursor = 0; effects = [];
          tree = exported.ChatScreen();
          // A guarded render-time state update is committed by the next render.
          if (dirty) continue;
          for (const effect of effects) {
            slots[effect.i]?.cleanup?.();
            slots[effect.i] = { deps: effect.deps, cleanup: effect.fn() };
          }
          if (first && options.strict) {
            for (const effect of effects) slots[effect.i]?.cleanup?.();
            for (const effect of effects) slots[effect.i].cleanup = effect.fn();
          }
          first = false;
        }
        for (const fn of microtasks.splice(0)) fn();
        await new Promise((resolve) => setImmediate(resolve));
        if (!dirty && !microtasks.length) return;
      }
      throw new Error("Chat screen did not settle");
    },
  };
}

test("plain Chat opens latest, while New Chat stays new", async () => {
  const page = screen("/chat");
  await page.flush();
  assert.equal(page.route.searchParams.get("c"), "latest");
  assert.equal(page.pane.activeId, "latest");
  page.pane.onNewChat();
  await page.flush();
  assert.equal(page.route.search, "?new=1");
  assert.equal(page.pane.activeId, null);
  assert.equal(page.starts.length, 0);
});

test("a dashboard question starts a new thread and streams exactly once during effect replay", async () => {
  const url = "/chat?new=1&documentId=doc&q=How+much+leave%3F&ask=launch";
  const page = screen(url, { strict: true });
  await page.flush();
  assert.equal(page.starts.length, 1);
  assert.equal(page.starts[0].data.documentId, "doc");
  assert.equal(page.sends.length, 1);
  assert.equal(page.sends[0].conversationId, "created");
  assert.equal(page.sends[0].question, "How much leave?");
  assert.equal(page.route.search, "?c=created");
  page.navigate(url);
  await page.flush();
  assert.equal(page.sends.length, 1);
  const reload = screen(url, { userId: page.user.id, storageMap: page.storageMap });
  await reload.flush();
  assert.equal(reload.sends.length, 0);
});

test("selecting a document keeps an incoming question and edits without sending it prematurely", async () => {
  const page = screen("/chat?q=Original+question");
  await page.flush();
  assert.equal(page.composer.value, "Original question");
  page.composer.onChange("Edited question");
  await page.flush();
  page.composer.onDocumentChange("doc");
  await page.flush();
  assert.equal(page.composer.value, "Edited question");
  assert.equal(page.sends.length, 0);
  page.composer.onSubmit();
  await page.flush();
  assert.equal(page.sends[0].question, "Edited question");
});

test("automatic submission waits for the document to be ready", async () => {
  const page = screen("/chat?new=1&documentId=doc&q=Keep+this&ask=wait", { documents: [] });
  await page.flush();
  assert.equal(page.composer.value, "Keep this");
  assert.equal(page.sends.length, 0);
  page.setDocuments([{ id: "doc", status: "Completed" }]);
  await page.flush();
  assert.equal(page.sends.length, 1);
});

test("a failed answer preserves the draft for manual retry in the same thread", async () => {
  const page = screen("/chat?new=1&documentId=doc&q=Retry+this&ask=retry", {
    stream: async (_, attempt) => { if (attempt === 1) throw new Error("Provider unavailable"); },
  });
  await page.flush();
  assert.equal(page.composer.value, "Retry this");
  assert.equal(page.composer.pending, false);
  assert.equal(page.sends.length, 1);
  page.composer.onSubmit();
  await page.flush();
  assert.equal(page.starts.length, 1);
  assert.equal(page.sends.length, 2);
});

test("stopping during thread creation restores the question and does not start a stream", async () => {
  let finishCreating;
  const created = new Promise((resolve) => { finishCreating = resolve; });
  const page = screen("/chat?new=1&documentId=doc&q=Keep+this&ask=stop", { create: () => created });
  await page.flush();
  page.composer.onStop();
  finishCreating();
  await page.flush();
  assert.equal(page.composer.pending, false);
  assert.equal(page.composer.value, "Keep this");
  assert.equal(page.sends.length, 0);
});

test("stopping after answer text arrives reloads the saved partial turn without a retry draft", async () => {
  const page = screen("/chat?new=1&documentId=doc&q=Do+not+duplicate&ask=partial", {
    stream: (request) => new Promise((_, reject) => {
      request.onDelta("A partial answer");
      request.signal.addEventListener("abort", () => reject(new DOMException("Stopped", "AbortError")));
    }),
  });
  await page.flush();
  page.composer.onStop();
  await page.flush();
  assert.equal(page.composer.pending, false);
  assert.equal(page.composer.value, "");
  assert.equal(page.sends.length, 1);
});

test("switching conversations cancels the old stream and ignores its late failure", async () => {
  let rejectStream;
  const result = new Promise((_, reject) => { rejectStream = reject; });
  const page = screen("/chat?new=1&documentId=doc&q=First+question&ask=switch", { stream: () => result });
  await page.flush();
  page.pane.onSelect("old");
  await page.flush();
  assert.equal(page.sends[0].signal.aborted, true);
  rejectStream(new Error("Late failure"));
  await page.flush();
  assert.equal(page.pane.activeId, "old");
  assert.equal(page.composer.value, "");
  assert.equal(page.composer.pending, false);
});
