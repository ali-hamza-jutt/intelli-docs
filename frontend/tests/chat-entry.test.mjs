import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import vm from "node:vm";
import ts from "typescript";

const code = ts.transpileModule(
  readFileSync(new URL("../src/lib/chatEntry.ts", import.meta.url), "utf8"),
  { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } },
).outputText;

function load() {
  const exports = {};
  vm.runInNewContext(code, { exports, URLSearchParams });
  return exports;
}
const entry = load();

test("Chat resumes the most recently updated conversation, regardless of creation order", () => {
  const older = { id: "older", updatedAt: "2026-10-01T10:00:00Z" };
  const latest = { id: "latest", updatedAt: "2026-10-04T10:00:00Z" };
  assert.equal(entry.latestConversation([older, latest]).id, "latest");
  assert.equal(entry.latestConversation([]), undefined);
  assert.equal(entry.shouldResumeLatest(new URLSearchParams()), true);
});

for (const query of ["c=chosen", "documentId=doc", "new=1", "q=question", "ask=launch"]) {
  test(`an explicit chat entry is never replaced by the latest conversation: ${query}`, () => {
    assert.equal(entry.shouldResumeLatest(new URLSearchParams(query)), false);
  });
}

test("preselects a single processed document, requires a choice for multiple documents", () => {
  const first = { id: "first", status: "Completed" };
  const second = { id: "second", status: "Completed" };
  const processing = { id: "processing", status: "Processing" };
  assert.equal(entry.selectedReadyDocument([first, processing], ""), first);
  assert.equal(entry.selectedReadyDocument([first, second], ""), undefined);
  assert.equal(entry.selectedReadyDocument([first, second], "second"), second);
  assert.equal(entry.selectedReadyDocument([first, second, processing], "processing"), undefined);
  assert.equal(entry.selectedReadyDocument([first, second], "deleted"), undefined);
  assert.equal(entry.selectedReadyDocument([], ""), undefined);
});

test("dashboard carries its question and document into an explicitly new chat", () => {
  const question = "What about A&B?\nAnd leave + pay / benefits?";
  const url = new URL(entry.dashboardChatUrl("doc", `  ${question}  `, "launch"), "http://localhost");
  assert.equal(url.pathname, "/chat");
  assert.equal(url.searchParams.get("q"), question);
  assert.equal(url.searchParams.get("documentId"), "doc");
  assert.equal(url.searchParams.get("new"), "1");
  assert.equal(url.searchParams.get("ask"), "launch");
  assert.equal(entry.shouldResumeLatest(url.searchParams), false);
});

test("dashboard submissions are claimed once across effects, reloads and Back/Forward", () => {
  const stored = new Map();
  const storage = { getItem: (key) => stored.get(key), setItem: (key, value) => stored.set(key, value) };
  const page = load();
  assert.equal(page.claimChatLaunch("alice", "launch", storage), true);
  assert.equal(page.claimChatLaunch("alice", "launch", storage), false);
  assert.equal(load().claimChatLaunch("alice", "launch", storage), false);
  assert.equal(page.claimChatLaunch("bob", "launch", storage), true);
  assert.equal(page.claimChatLaunch("alice", "another-launch", storage), true);
});

test("blocked browser storage still prevents a repeated effect from sending twice", () => {
  const storage = { getItem: () => { throw new Error("Storage disabled"); } };
  const page = load();
  assert.equal(page.claimChatLaunch("alice", "launch", storage), true);
  assert.equal(page.claimChatLaunch("alice", "launch", storage), false);
});
