import { defineConfig } from "orval";

/**
 * Generates typed TanStack Query hooks from the API's own OpenAPI document.
 * Regenerate with `npm run api:sync` whenever the backend contract changes — TypeScript
 * then reports every call site that no longer matches.
 */
export default defineConfig({
  documind: {
    input: "./openapi.json",
    output: {
      mode: "tags-split",
      target: "./src/lib/api/generated",
      schemas: "./src/lib/api/model",
      client: "react-query",
      httpClient: "axios",
      override: {
        // Every request routes through our own wrapper, which attaches the access token
        // and retries once after a silent refresh.
        mutator: {
          path: "./src/lib/api/client.ts",
          name: "apiRequest",
        },
        query: {
          signal: true,
        },
      },
    },
  },
});
