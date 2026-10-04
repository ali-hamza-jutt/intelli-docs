# DocuMind Manual Test Plan

This plan covers the current web application, API behavior that affects the UI, and the main end-to-end user journeys. Run P0 cases before a release, P0 and P1 for a full regression, and P2 for broader browser, accessibility, security, and resilience coverage.

## Functionality map

| Area | Current functionality |
|---|---|
| Public site | Product sections, feature explanation, FAQ, sign-in and registration links |
| Authentication | Register, sign in, token refresh, sign out, protected routes, profile update, password change |
| Dashboard | Document counts, recent documents, recent conversations, upload entry, question handoff to chat |
| Documents | PDF upload, validation, background processing status, list search/filter/sort, detail, download, delete, reprocess |
| Document inspection | Metadata, page-by-page extracted text, chunk boundaries, chunk pagination |
| Chat | Latest conversation resume, new chat, document selection, saved conversations, streaming answers, stop, retry, copy, citations |
| Usage | Monthly chat calls, input/output tokens, embedding calls and reported embedding tokens |
| Responsive shell | Desktop sidebar, collapsed sidebar, mobile navigation, account menu, notifications message |
| API safeguards | Ownership isolation, input validation, consistent errors, per-user upload and AI rate limits |

## Test setup

Use the frontend URL shown by the dev server, normally `http://localhost:3000` or `http://localhost:3001`, and the API at `http://localhost:5100`.

Prepare:

- Two accounts: User A and User B.
- A 2 to 5 page text PDF containing unique, easy-to-check facts on different pages.
- A second valid PDF with unrelated content.
- A valid PDF close to 20 MB.
- A PDF larger than 20 MB.
- A zero-byte `.pdf` file.
- A non-PDF renamed to `.pdf`.
- A scanned image-only PDF with no text layer.
- A filename containing spaces, punctuation, and non-ASCII characters.
- Browser widths near 375 px, 768 px, 1280 px, and 1536 px.

Record the build or commit, browser, result, evidence, and defect link for each run. Use `Pass`, `Fail`, `Blocked`, or `Not run`.

## P0 release smoke flow

1. Open the landing page and register User A.
2. Confirm registration signs the user in and opens the dashboard.
3. Upload the text PDF and verify the progress UI completes without an `UPLOAD_NOT_FOUND` error.
4. Close the upload dialog while processing and verify processing continues.
5. Wait for the document to move from Uploaded or Processing to Completed.
6. Open the document and verify metadata, extracted pages, chunks, and download.
7. From the dashboard, type a question, select the document, and submit.
8. Verify chat opens with the original question already present and submits it exactly once.
9. Verify the streamed answer is grounded in the PDF and citations open the correct source text and page.
10. Ask a follow-up that depends on the previous answer, then reload and verify the conversation persists.
11. Navigate away, click Chat in the sidebar, and verify the most recently updated conversation opens automatically.
12. Sign out and verify protected pages return the user to sign in.

## Public site

| ID | Pri | Test | Expected result |
|---|---|---|---|
| PUB-01 | P1 | Open `/` as a signed-out visitor. | Landing content renders without console errors or broken layout. |
| PUB-02 | P1 | Use each top navigation link. | The page scrolls to the matching section and the URL fragment is correct. |
| PUB-03 | P1 | Click Get Started and sign-in actions. | Registration and login pages open. |
| PUB-04 | P2 | Expand and collapse every FAQ item. | Only the intended answer changes state; text stays readable. |
| PUB-05 | P1 | Inspect the three How it works step labels and all visible app copy. | No em dash character is displayed. |
| PUB-06 | P2 | Navigate the page with Tab and activate controls with Enter or Space. | Focus is visible and interactive elements work without a mouse. |
| PUB-07 | P2 | Test at mobile, tablet, and desktop widths. | Content does not overlap, clip, or create unwanted horizontal scrolling. |

## Authentication and session

| ID | Pri | Test | Expected result |
|---|---|---|---|
| AUTH-01 | P0 | Register with a valid name, unique email, matching password, and accepted terms. | Account is created, a success toast appears, and dashboard opens. |
| AUTH-02 | P1 | Register with the same email again. | A clear duplicate-account error appears and no second account is created. |
| AUTH-03 | P1 | Try empty, one-character, and over-120-character names. | The matching validation message appears. |
| AUTH-04 | P1 | Try an invalid or over-256-character email. | The email is rejected with a readable message. |
| AUTH-05 | P1 | Try passwords under 8 characters, over 128 characters, letters only, and numbers/symbols only. | Each invalid password is rejected; a mixed password is accepted. |
| AUTH-06 | P1 | Enter different password and confirmation values. | Submission stops and “The two passwords do not match.” appears. |
| AUTH-07 | P2 | Change the registration password strength while typing. | The strength meter updates without affecting the entered value. |
| AUTH-08 | P0 | Sign in with correct credentials. | User enters the dashboard and sees a Signed in toast. |
| AUTH-09 | P1 | Sign in with a wrong password and with an unknown email. | A generic authentication error appears and does not reveal whether the account exists. |
| AUTH-10 | P1 | Open a protected URL while signed out, then sign in. | Login opens and returns the user to the originally requested page. |
| AUTH-11 | P0 | Reload a protected page after sign-in. | The refresh cookie restores the session without flashing protected content to a signed-out user. |
| AUTH-12 | P1 | Open the account menu and sign out. | The session ends, login opens, and Back cannot expose protected data. |
| AUTH-13 | P1 | Leave the app open past the access-token lifetime, then perform an action. | Refresh occurs transparently and the action completes once. |
| AUTH-14 | P1 | Change the password in Settings, then try the old and new passwords. | All sessions are revoked; old password fails and new password succeeds. |
| AUTH-15 | P2 | Submit the login or registration form repeatedly while it is pending. | Only one request is accepted and controls stay disabled until completion. |

## App shell and navigation

| ID | Pri | Test | Expected result |
|---|---|---|---|
| NAV-01 | P1 | Visit Dashboard, Documents, Chat, and Settings from the desktop sidebar. | Each route opens and the active section is visually identified. |
| NAV-02 | P1 | Collapse and expand the sidebar. | Width and labels change cleanly; icon links retain tooltips and accessibility names. |
| NAV-03 | P1 | At mobile width, use the bottom navigation. | All primary routes open and the selected route is highlighted. |
| NAV-04 | P1 | Open the avatar menu, click outside, reopen it, then choose Settings. | Click-away closes it and Settings navigation works. |
| NAV-05 | P2 | Click Notifications. | A “No new notifications” toast appears once. |
| NAV-06 | P1 | Inspect the browser title and all shell text. | The title uses a vertical bar and no visible em dash appears. |
| NAV-07 | P2 | Use browser Back and Forward across app routes. | URL, heading, active navigation, and page content stay synchronized. |

## Dashboard

| ID | Pri | Test | Expected result |
|---|---|---|---|
| DASH-01 | P0 | Open the dashboard with a new account. | Greeting uses the profile first name; counts are zero and empty states appear. |
| DASH-02 | P1 | Upload completed, processing, and failed documents. | Documents, Processed, Processing, and Failed counts match the list. |
| DASH-03 | P1 | Open a recent document and a recent conversation. | The correct detail page or conversation opens. |
| DASH-04 | P0 | Enter a question when exactly one completed document exists. | That document is preselected and the question can be submitted. |
| DASH-05 | P0 | Enter a question when multiple completed documents exist without selecting one. | The question stays in the box and focus moves to the document selector. |
| DASH-06 | P0 | Select a completed document and submit the dashboard question. | Chat opens, keeps the exact trimmed question, creates a conversation, and sends once. |
| DASH-07 | P0 | Reload or use Back and Forward after DASH-06. | The dashboard handoff does not create a duplicate message or answer. |
| DASH-08 | P1 | Click a suggested question. | It uses the selected document and follows the same one-time handoff flow. |
| DASH-09 | P1 | Try asking when no completed document exists. | The UI explains that a processed document is required and keeps the question. |
| DASH-10 | P1 | Leave a document processing while watching the dashboard. | Polling updates status and counts without a manual reload. |

## Upload and ingestion

| ID | Pri | Test | Expected result |
|---|---|---|---|
| UPL-01 | P0 | Upload a valid text PDF with Browse Files. | Progress reaches 100%, confirmation succeeds, and a document is created. |
| UPL-02 | P1 | Drag and drop a valid PDF. | Drag styling appears and upload behaves like Browse Files. |
| UPL-03 | P0 | Observe the network requests for a cloud upload. | `upload-ticket`, provider upload, and `confirm` succeed; confirm does not return `UPLOAD_NOT_FOUND`. |
| UPL-04 | P1 | Test with cloud storage unconfigured so local storage is used. | Ticket fallback uses multipart `/api/Documents/upload` and the document is created. |
| UPL-05 | P1 | Upload an empty PDF. | Upload is rejected with an empty-file message. |
| UPL-06 | P1 | Upload a file larger than 20 MB. | Upload is rejected before transfer with the size and 20 MB limit shown. |
| UPL-07 | P1 | Choose a `.txt`, `.docx`, image, or executable file. | Client rejects it because the current upload feature supports PDF only. |
| UPL-08 | P1 | Upload a non-PDF renamed to `.pdf`. | Server content validation rejects it; no usable document is indexed. |
| UPL-09 | P1 | Upload a scanned PDF with no text layer. | Processing moves to Failed with a useful explanation and no chat action is enabled. |
| UPL-10 | P1 | Upload a filename with spaces and non-ASCII characters. | The original name displays correctly and download preserves a safe filename. |
| UPL-11 | P1 | Cancel while bytes are uploading. | Transfer stops, the dialog returns to its initial state, and no false success appears. |
| UPL-12 | P0 | Close the dialog after upload while processing is active. | Processing continues in the background and status updates elsewhere. |
| UPL-13 | P1 | Close and reopen the upload dialog after success or failure. | A fresh idle upload form appears. |
| UPL-14 | P1 | Simulate provider, API, or network failure during each upload stage. | A readable error appears, no false completion is shown, and retry is possible. |
| UPL-15 | P2 | Let a signed upload ticket expire before provider upload or confirmation. | The upload fails safely with an actionable error and no orphaned document row is confirmed. |
| UPL-16 | P2 | Exceed 20 upload requests within one minute for one user. | Further requests return 429 with `RATE_LIMITED` and Retry-After; another user is unaffected. |

## Documents list

| ID | Pri | Test | Expected result |
|---|---|---|---|
| DOC-01 | P0 | Open Documents with several items. | The user’s documents load newest first with accurate names and statuses. |
| DOC-02 | P1 | Search by full name, partial name, mixed case, and no match. | Matching rows filter locally; no-match state is clear. |
| DOC-03 | P1 | Filter All, Processing, Completed, and Failed. | Only matching states remain and counts/data do not change. |
| DOC-04 | P1 | Sort by Newest, Oldest, and Name. | Row order matches the selected sort. |
| DOC-05 | P1 | Combine search and a status filter, then clear filters. | Both conditions apply and Clear filters restores the list. |
| DOC-06 | P1 | Open a row using its name and open action. | Both open the same correct document detail page. |
| DOC-07 | P1 | Download a document from its row. | Original bytes download successfully for the owner. |
| DOC-08 | P0 | Delete a document from its row. | The row disappears, a success toast appears, and the file is no longer retrievable. |
| DOC-09 | P1 | Trigger list loading and API error states. | Skeletons appear while loading; error state offers a working retry. |
| DOC-10 | P1 | Leave a document processing on the list. | Polling stops after it becomes Completed or Failed. |
| DOC-11 | P2 | Upload two files with the same visible filename. | Both rows remain independently addressable by their IDs. |

## Document detail and processing

| ID | Pri | Test | Expected result |
|---|---|---|---|
| DET-01 | P0 | Open a completed document. | Name, status, type, size, upload date, processed date, and chunk count are accurate. |
| DET-02 | P1 | Open a processing document. | Status polls, unavailable values read “Not available,” and chat is disabled. |
| DET-03 | P1 | Open a failed document. | Failure reason and Try again action appear; extracted text explains it is unavailable. |
| DET-04 | P1 | Reprocess a failed document after correcting the underlying configuration. | Processing reruns and detail, text, chunks, and list status refresh. |
| DET-05 | P0 | Inspect extracted text in a multi-page PDF. | Page count, word count, page labels, paragraph breaks, and content match the PDF. |
| DET-06 | P1 | Use Show more and Show less on extracted text. | Preview height changes without losing position or content. |
| DET-07 | P1 | Inspect chunks. | Chunk order, page ranges, overlap highlighting, and total count are coherent. |
| DET-08 | P1 | Use Previous and Next on more than ten chunks. | Correct chunk pages load; buttons disable at first and last pages. |
| DET-09 | P0 | Click Chat with document. | A new chat opens with that completed document selected. |
| DET-10 | P1 | Click each suggested document action. | It opens chat scoped to the same document. |
| DET-11 | P0 | Download from detail. | Owner receives the original document. |
| DET-12 | P0 | Delete from detail. | User returns to Documents and all text/chunks/file access for that document is removed. |
| DET-13 | P1 | Open a deleted, random, or another user’s document ID. | A 404-style not-found screen appears without revealing ownership. |
| DET-14 | P2 | Refresh detail during each processing state. | State restores correctly and no duplicate processing job is created. |

## Chat and conversations

| ID | Pri | Test | Expected result |
|---|---|---|---|
| CHAT-01 | P0 | Create at least two conversations, update the older one, leave Chat, then click Chat in the sidebar. | The most recently updated conversation opens automatically. |
| CHAT-02 | P0 | Click a specific recent conversation from Dashboard or the conversation list. | The requested conversation opens, not merely the latest one. |
| CHAT-03 | P0 | Click New Chat. | Current stream stops, draft clears, and an empty chat is shown. |
| CHAT-04 | P1 | Open Chat with no conversations or selected document. | Empty state asks for a document and Browse documents works. |
| CHAT-05 | P1 | Select a completed document in a new chat. | Composer becomes enabled and the document remains selected in the URL. |
| CHAT-06 | P1 | Verify processing and failed documents in the selector. | Only completed documents are available. |
| CHAT-07 | P0 | Send with the button and with Enter; use Shift+Enter for a newline. | Send methods submit once and Shift+Enter only inserts a newline. |
| CHAT-08 | P1 | Submit blank, whitespace, 1 to 2 character, punctuation-only, and over-2000-character questions. | Invalid questions do not create valid turns and readable validation appears where a request reaches the API. |
| CHAT-09 | P0 | Ask a fact clearly present in the PDF. | Answer streams progressively and cites relevant source passages. |
| CHAT-10 | P0 | Ask a question the selected PDF cannot answer. | App gives a grounded refusal and does not fabricate an answer; no model usage is recorded if retrieval finds no context. |
| CHAT-11 | P0 | Ask a follow-up using a pronoun or omitted subject. | Answer uses recent conversation history and remains grounded in the document. |
| CHAT-12 | P1 | Click a suggestion in an empty document-scoped chat. | The suggestion sends once and creates a saved conversation. |
| CHAT-13 | P0 | Reload after completed turns. | User messages, assistant messages, titles, and citations persist. |
| CHAT-14 | P1 | Stop before the first answer token. | Request cancels and the original question remains available to retry. |
| CHAT-15 | P1 | Stop after answer text starts. | Partial answer is saved and reload shows the stopped turn consistently. |
| CHAT-16 | P1 | Switch conversations while an answer is streaming. | Old stream is cancelled and cannot append text to the newly opened conversation. |
| CHAT-17 | P1 | Click Copy on an answer. | Clipboard contains the answer and a confirmation toast appears. |
| CHAT-18 | P1 | Click Ask again on an answer. | The last user question is sent again once. |
| CHAT-19 | P0 | Open every citation. | Marker, filename, page range, match score, and passage match the cited answer; desktop rail can open the document. |
| CHAT-20 | P1 | Search the conversation list by title or document name. | Matching conversations remain and no-result feedback appears. |
| CHAT-21 | P0 | Delete an inactive conversation, then the active conversation. | Each disappears; deleting the active one returns to a clean chat state. |
| CHAT-22 | P1 | Delete a document that has a saved conversation, then reopen the conversation. | Existing messages and sources remain; warning appears and new questions are unavailable. |
| CHAT-23 | P1 | Simulate AI timeout, quota, invalid credentials, and network loss. | Error is readable, draft is restored, and the user can retry without a duplicate turn. |
| CHAT-24 | P2 | Exceed 12 AI calls within one minute for one user. | Further requests return 429 with retry guidance; another user’s allowance is unaffected. |
| CHAT-25 | P2 | Open a chat URL with a random or another user’s conversation ID. | Access returns not found and does not expose messages or document data. |

## Settings and usage

| ID | Pri | Test | Expected result |
|---|---|---|---|
| SET-01 | P1 | Switch among Profile, Security, and Usage. | Correct panel appears and no stale panel remains visible. |
| SET-02 | P0 | Change the profile name to a valid value. | Success toast appears; settings, avatar initials, sidebar, and dashboard greeting update. |
| SET-03 | P1 | Try empty, unchanged, too-short, and too-long names. | Save is disabled when appropriate or the API returns the matching validation message. |
| SET-04 | P1 | Inspect the email field. | It is read-only and explains that email cannot be changed. |
| SET-05 | P0 | Change password with the correct current password and a valid new password. | Password updates, all sessions are revoked, and login opens. |
| SET-06 | P1 | Try a wrong current password, the same password, and invalid new passwords. | Clear errors appear and the session remains active. |
| SET-07 | P1 | Open Usage before any AI work. | Valid zero or provider-not-reported values display without misleading blanks. |
| SET-08 | P1 | Upload/index a document and complete chat answers, then revisit Usage. | Embedding and chat counters increase consistently with provider-reported usage. |
| SET-09 | P2 | Force the usage request to fail. | A contained “Could not load your usage just now.” state appears. |

## Security, ownership, and API behavior

| ID | Pri | Test | Expected result |
|---|---|---|---|
| SEC-01 | P0 | As User B, request User A’s document detail, status, text, chunks, download, reprocess, delete, and document chat endpoints. | Every request returns 404-style isolation and reveals no content or existence. |
| SEC-02 | P0 | As User B, request User A’s conversation, messages, stream, and delete endpoints. | Every request is denied without exposing thread metadata or messages. |
| SEC-03 | P1 | Call protected endpoints without a token and with an invalid or expired token. | API returns 401 and the UI returns to login when recovery fails. |
| SEC-04 | P1 | Reuse a rotated refresh token. | Token reuse is rejected and the affected refresh-token family is revoked. |
| SEC-05 | P1 | Use filenames containing `/`, `\\`, or `..`. | Validation rejects traversal-like names and no file is written outside storage. |
| SEC-06 | P1 | Send malformed JSON, invalid GUIDs, missing fields, and wrong content types. | API returns consistent ProblemDetails with status, detail, errorCode, and traceId. |
| SEC-07 | P1 | Confirm a nonexistent, expired, or another user’s upload reference. | API returns `UPLOAD_NOT_FOUND`; no document row is created. |
| SEC-08 | P1 | Attempt to confirm a provider asset whose type, size, or owner folder differs from the ticket. | Confirmation is rejected and the asset is not accepted into the library. |
| SEC-09 | P1 | Check responses and logs after login, refresh, upload, and chat. | Tokens, passwords, document text, questions, and answers are absent from logs and error payloads. |
| SEC-10 | P2 | Send requests from an origin outside the configured CORS allowlist. | Browser blocks credentialed cross-origin use. |
| SEC-11 | P2 | Open `/health`, `/api/health`, OpenAPI, and Swagger in Development. | Health returns status; development API documentation loads; production exposure follows configuration. |

## Responsive, accessibility, and resilience

| ID | Pri | Test | Expected result |
|---|---|---|---|
| UX-01 | P1 | Run core P0 flow at 375 px, 768 px, 1280 px, and 1536 px. | Controls remain visible, readable, and usable without accidental horizontal overflow. |
| UX-02 | P1 | Open a citation below 1536 px and at 1536 px or wider. | Narrow view uses a bottom sheet; wide view uses the source rail. |
| UX-03 | P1 | Complete register, upload, document navigation, chat, and settings using only the keyboard. | Focus order is logical; dialogs and controls can be operated without a mouse. |
| UX-04 | P1 | Open and close the upload modal with keyboard and mouse. | Focus is contained while open and returns to a sensible trigger when closed. |
| UX-05 | P1 | Trigger form, upload, list, chat, and API errors. | Errors are announced or exposed with alert semantics and are not color-only. |
| UX-06 | P2 | Zoom to 200% and test Windows high-contrast mode. | Text and controls remain readable and no critical action is clipped. |
| UX-07 | P2 | Test Chrome, Edge, and Firefox current versions. | P0 flow behaves consistently, including streaming, clipboard, cookies, and download. |
| UX-08 | P1 | Throttle to a slow network during upload, page loads, and chat. | Progress, skeleton, thinking, stop, and retry states remain accurate. |
| UX-09 | P1 | Restart the frontend during a saved conversation. | Reload restores session and saved content. |
| UX-10 | P1 | Restart the API while a document is processing, then start it again. | Unfinished work is requeued and settles without creating duplicate documents. |
| UX-11 | P2 | Open the same account in two tabs and update profile, documents, and conversations. | Each tab becomes consistent after refetch or reload without data corruption. |
| UX-12 | P2 | Check browser console and network panel during every P0 case. | No unhandled exceptions, failed background loops, duplicate mutations, or sensitive payload leaks appear. |

## Current product gaps to track separately

These items are present in the UI or marketing but are not complete product features. Do not count them as passing functional coverage until their implementation exists.

| Gap | Current behavior |
|---|---|
| Password reset | Forgot and reset screens show “not available yet”; no email or reset endpoint exists. |
| Single sign-on | Control is disabled and marked coming soon. |
| Header document search | The header field is visual only; the Documents page filename search works. |
| Remember me | The checkbox is shown, but it does not currently select a different session lifetime. |
| Notifications | The bell always reports no new notifications. |
| Upload formats | The working upload path accepts PDF only, while parts of the landing page still advertise DOCX and TXT. |
| Collections and workspace invites | Marketing mentions collection or workspace concepts, but management flows are not implemented. |
| Suggested document actions | Actions open a document-scoped chat but do not prefill a distinct command. |

## Automated checks to run with manual testing

```powershell
cd D:\projects\intelli-docs\frontend
npm test
npm run lint
npx tsc --noEmit
npm run build

cd D:\projects\intelli-docs\backend
dotnet test DocuMind.slnx --no-restore
```

The frontend test suite includes a guard that scans TypeScript and JSX user-facing text and fails if the em dash character is introduced again.
