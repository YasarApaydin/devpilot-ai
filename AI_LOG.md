# 🤖 DevPilot AI — AI Development & Verification Log (`AI_LOG.md`)

> **Document Scope:** This log documents the engineering, hardening, and verification iterations conducted on the **DevPilot AI** platform. It provides technical transparency for contributors, auditors, and maintainers regarding architectural decisions, security boundaries, and concurrency guarantees.

---

## 📌 Executive Summary

**DevPilot AI** is a production-ready, full-stack AI workspace and exterior security diagnostics platform. It combines conversational intelligence (LLM streaming, Whisper voice transcription, and RAG document knowledge) with an automated, read-only external security assessment engine.

The platform is designed following **Clean Architecture (Onion Architecture)**, **CQRS (Command Query Responsibility Segregation via MediatR)**, and **Domain-Driven Design (DDD)** principles on .NET 9 and React 19.

---

## 🛡️ Core Hardening & Engineering Milestones

### 1. Exterior Security Scanner Engine (`SecurityScanRunnerService`)
- **Non-Intrusive, Read-Only Boundary:** The scanner operates strictly on the public external surface (HTTP security headers, TLS configuration, CORS policies, DNS records, `robots.txt`, `security.txt`, and exposed client assets). Active penetration testing (SQL injection fuzzing, brute force, exploit testing, form fuzzing) is strictly disallowed by architecture.
- **SSRF & Private IP Containment:** All target hostnames are resolved through DNS and checked against RFC 1918, RFC 3927 (link-local), loopback (`127.0.0.0/8`), IPv6 link-local, and cloud metadata IP ranges (`169.254.169.254`). Any target resolving to a private/internal network is rejected fail-closed.
- **Strict Redirect Validation:** The HTTP client validates each hop in a redirect chain:
  - Scheme, host, and port must match the target's authorized scope.
  - Port traversal (e.g., redirecting to unauthorized internal ports) is blocked.
  - Only safe HTTP → HTTPS transitions within the same host are permitted.
- **Heuristic Severity Capping:** Passive findings generated via LLM analysis are capped at **Medium severity (CVSS $\le$ 6.9)** to prevent unverified Critical/High hallucinations in external diagnostics.

---

### 2. Atomic Quota Engine & Concurrency Resilience (`UserQuotaRepository`, `QuotaService`)
- **Database-Level Atomic Rollover:** In relational environments (PostgreSQL), daily cycle resets are executed directly in the database via `ExecuteUpdateAsync` (`WHERE CycleStartDate < today`), resetting used credits and updating the cycle timestamp in a single atomic SQL statement.
- **Race Condition Elimination:** Resolved the concurrency issue where simultaneous reads (`GetQuotaStatusAsync`) and credit consumptions could overwrite each other:
  - `GetQuotaStatusAsync` performs an atomic DB-level rollover before reading data.
  - Entity reads for status are performed with `AsNoTracking` to prevent EF Core change-tracker stale-state overwrite.
  - Credit consumption executes atomically (`CreditsUsedToday < DailyCreditLimit`).
- **Relational Test Verification:** Verified the `ExecuteUpdateAsync` relational path directly against real PostgreSQL databases (`IsRelational == true`), ensuring no divergence between production SQL execution and test environments.

---

### 3. Single Authoritative Source of Truth for Environment (`AppEnvironment`)
- **Precedence Hierarchy:** Replaced multi-candidate blending with a strict, single authoritative source of truth:
  $$\text{hostEnvironmentName} \longrightarrow \text{ASPNETCORE\_ENVIRONMENT} \longrightarrow \text{DOTNET\_ENVIRONMENT} \longrightarrow \text{DEPLOYMENT\_STAGE} \longrightarrow \text{"Production (Fail-Closed)"}$$
- **Cryptographic Gateways:** In any production-like environment, static developer default keys for JWT signing and AES-256 API key encryption are prohibited, requiring a minimum of 32 characters of high-entropy key material.

---

### 4. Account-Centric Rate Limiting (`Program.cs`)
- **User Identity Partitioning:** Upgraded rate-limiting partitions on heavy workloads (AI chat, voice processing, security scanning) from pure client IP addresses to authenticated user IDs (`user:{userId}`).
- **Anonymous Fallback:** Unauthenticated requests safely fall back to `ip:{remoteIp}`. This prevents account abuse via IP hopping or rotating proxies while maintaining fair-use protection for public endpoints.

---

### 5. Truthful Error Messaging & Idempotent Voice Refunds (`SendVoiceMessageCommand`)
- **Quota Check Before Transcription:** User credit verification and atomic consumption occur prior to calling expensive audio transcription (Whisper) services.
- **Accurate Refund Tracking:** `TryRefundAsync` captures the atomic boolean result of `RefundCreditAsync`:
  - **Credit Consumed & Refund Succeeded:** User message confirms credit refund.
  - **Credit Consumed & Refund Failed:** User message transparently informs the user that refund failed and prompts support contact, rather than falsely claiming a refund succeeded.
  - **BYOK (Bring Your Own Key):** When user-provided API keys bypass credit consumption, no misleading refund messages are presented.
- **Exception Sanitization:** Raw stack traces, system paths, and internal error details are logged securely to server logs and masked from the client API response.

---

### 6. UI Pagination & Query Scoping
- **Endpoint Pagination:** Synchronized backend pagination parameters (`page`, `pageSize`) across the React API layer (`conversationApi.ts`), conversation hook (`useConversations.ts`), and chat hook (`useChat.ts`).
- **Selective Scan Findings Loading:** `GetConversationMessagesQuery` scopes security scan loading strictly to the scans referenced by the messages in the requested page, preventing N+1 database bottlenecks and eliminating memory bloat on long conversations.

---

## 🧪 Verification Matrix

| Area / Module | Test Suite / Verification Method | Status |
| :--- | :--- | :--- |
| **Birim & Entegrasyon Testleri** | `dotnet test WebChatAI.Tests.csproj` (147 test) | ✅ **147 / 147 Passed** |
| **İlişkisel Veritabanı Atomik Kota** | `QuotaService_RelationalDatabase_ConcurrentResetAndConsumption` | ✅ **Passed** |
| **SSRF & Güvenli Yönlendirme** | `SecurityScanRunnerService` Safe GET & Redirect Suite | ✅ **Passed** |
| **Sesli Mesaj İade Dürüstlüğü** | `SendVoiceMessageCommand_WhenRefundFails_DoesNotFalselyClaimRefund` | ✅ **Passed** |
| **Ortam Tek Güvenilir Kaynak** | `AppEnvironment_SingleAuthoritativeSource_Precedence` | ✅ **Passed** |
| **Frontend Production Derlemesi** | `tsc -b && vite build` (React-UI) | ✅ **Built Cleanly** |
| **Docker Compose Servisleri** | PostgreSQL 16, .NET 9 WebApi, Nginx Frontend | ✅ **Healthy & Running** |

---

## 🔒 Confidentiality & Safe Handling

- **No Secrets in Source:** No private tokens, API keys, passwords, database credentials, or host-specific machine paths are stored or committed.
- **Secure Defaults:** All configuration parameters are sourced from environment variables with safe development templates in `.env.example`.
- **Git Hygiene:** Root `.gitignore` ensures build directories (`bin/`, `obj/`, `dist/`), temporary logs, test results, local IDE settings, and secret `.env` files remain outside source control.
