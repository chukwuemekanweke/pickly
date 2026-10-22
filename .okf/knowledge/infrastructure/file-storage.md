---
type: infrastructure
title: File Upload and Object Storage Architecture (Cloudflare R2)
description: IObjectStorageService/IObjectStorageProvider abstraction over Cloudflare R2, presigned upload flow, and the avatar/file-upload feature shape
resource: okf://knowledge/infrastructure/file-storage
tags:
    - constraint
    - infrastructure
    - storage
    - file-upload
    - cloudflare-r2
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Infrastructure/Storage/**
    - src/BLVK.Pickly.Logistics.Domain/Common/Storage/**
    - src/BLVK.Pickly.Logistics.Domain/Common/FileUploads/**
    - src/BLVK.Pickly.Logistics.Application/Common/FileUploads/**
    - src/BLVK.Pickly.Logistics.Application/Stakeholders/AvatarUploads/**
    - src/BLVK.Pickly.Logistics.Contracts/Commands/Storage/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Infrastructure/Storage/CloudflareR2ObjectStorageProvider.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Shape

- Application/Domain depend on `IObjectStorageService`/`IObjectStorageProvider` (Domain-defined), never on Cloudflare R2 SDK types directly. `CloudflareR2ObjectStorageProvider` (built via
  `ICloudflareR2ClientFactory`) is the real implementation; `NoopObjectStorageProvider` is used where object storage is not configured (e.g. some test/dev paths).
- Runtime provider selection is data-driven through the active `FileStorage` row in `infrastructure.Providers`. The Cloudflare implementation and seed data use the exact key
  `cloudflare_r2`; the template seed activates it and deactivates `noop`. Docker Compose forwards the seven `CLOUDFLARE_R2_*` launcher variables into the corresponding
  `ObjectStorage__CloudflareR2__*` .NET configuration keys for WebAPI, Consumer, and Jobs.
- `PublicBaseUrl` is required. URLs returned for objects uploaded or promoted to the public bucket are always built from this public development/custom-domain base URL, never from the
  private S3 API endpoint and bucket name.
- Uploads use a presigned-URL flow: the client requests a presigned upload (`ObjectStoragePresignedUploadRequest`/`Result`), uploads directly to R2, then the app promotes the object from a
  quarantine/staging visibility to its final visibility (`ObjectStoragePromotionRequest`, `ObjectStorageVisibility`) once validated. This is the pattern the `AvatarUploads` and `Common/FileUploads`
  features implement — a `FileUploadSession` entity tracks the lifecycle.
- Because the browser sends the presigned `PUT` directly to the private R2 bucket, API CORS does not cover that hop.
  R2 CORS is managed outside this repository and production must allow only `https://app.pikly.org`, method `PUT`, and
  request header `Content-Type`; expose `ETag` so the client can read the upload result. Do not use a wildcard production origin.
- Rejected/orphaned uploads are cleaned up asynchronously via `Contracts.Commands.Storage.DeleteQuarantinedObject`, dispatched through the outbox (see [[transactional-outbox]]), not deleted synchronously in the request path.

## Constraint

New file-upload features reuse `IObjectStorageService` and the existing presign → upload → promote lifecycle rather than inventing a new upload mechanism per feature.
