import assert from "node:assert/strict";
import { readdir, readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";
import test from "node:test";
import ts from "typescript";

const SOURCE_ROOT = fileURLToPath(new URL("../src", import.meta.url));

async function sourceFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = await Promise.all(
    entries.map(async (entry) => {
      const target = path.join(directory, entry.name);
      if (entry.isDirectory()) return sourceFiles(target);
      return /\.tsx?$/.test(entry.name) ? [target] : [];
    }),
  );

  return files.flat();
}

test("user-facing frontend copy does not contain em dashes", async () => {
  const failures = [];

  for (const file of await sourceFiles(SOURCE_ROOT)) {
    const sourceText = await readFile(file, "utf8");
    const sourceFile = ts.createSourceFile(
      file,
      sourceText,
      ts.ScriptTarget.Latest,
      true,
      file.endsWith(".tsx") ? ts.ScriptKind.TSX : ts.ScriptKind.TS,
    );

    const visit = (node) => {
      const isCopy = ts.isStringLiteralLike(node) || ts.isJsxText(node);
      if (isCopy && node.getText(sourceFile).includes("—")) {
        const { line, character } = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
        failures.push(`${path.relative(SOURCE_ROOT, file)}:${line + 1}:${character + 1}`);
      }
      ts.forEachChild(node, visit);
    };

    visit(sourceFile);
  }

  assert.deepEqual(failures, [], `Em dash found in user-facing copy:\n${failures.join("\n")}`);
});
