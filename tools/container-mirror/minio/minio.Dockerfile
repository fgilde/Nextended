# MinIO server RELEASE.2025-09-07T16-13-09Z, rebuilt because MinIO stopped publishing images
# (Docker Hub, quay.io and dl.min.io are gone; the GitHub release still carries the RPM).
#
# The base is the last official image still on hand — same UBI9-micro layout, same
# docker-entrypoint.sh that prepends "minio" to the arguments. Only /usr/bin/minio is
# swapped for the binary from the official release RPM, verified by its published checksum.

FROM alpine:3.20 AS unpack
RUN apk add --no-cache libarchive-tools
ADD --checksum=sha256:8b9df992b1e90063489fd90117811e7a50f7f4473ea229df5d4e413c141d3028 \
    https://github.com/minio/minio/releases/download/RELEASE.2025-09-07T16-13-09Z/minio-20250907161309.0.0-1.x86_64.rpm \
    /tmp/minio.rpm
RUN mkdir /out && bsdtar -xf /tmp/minio.rpm -C /out

FROM ghcr.io/fgilde/minio:RELEASE.2025-04-22T22-12-26Z
COPY --from=unpack --chmod=0755 /out/usr/local/bin/minio /usr/bin/minio
LABEL version="RELEASE.2025-09-07T16-13-09Z" \
      release="RELEASE.2025-09-07T16-13-09Z" \
      org.opencontainers.image.title="MinIO server (mirror)" \
      org.opencontainers.image.version="RELEASE.2025-09-07T16-13-09Z" \
      org.opencontainers.image.licenses="AGPL-3.0-only" \
      org.opencontainers.image.source="https://github.com/fgilde/Nextended" \
      org.opencontainers.image.description="Unmodified MinIO server RELEASE.2025-09-07T16-13-09Z from the official release RPM. Upstream source: https://github.com/minio/minio/tree/RELEASE.2025-09-07T16-13-09Z"
