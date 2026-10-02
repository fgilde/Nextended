# MinIO client (mc) RELEASE.2025-08-13T08-35-41Z, rebuilt because MinIO stopped publishing images.
#
# The base is the last official mc image still on hand — it keeps the shell the bucket-init
# container runs its script with. Only /usr/bin/mc is swapped for the official release binary,
# verified by its published checksum.

FROM ghcr.io/fgilde/minio-mc:RELEASE.2025-04-16T18-13-26Z
ADD --checksum=sha256:01f866e9c5f9b87c2b09116fa5d7c06695b106242d829a8bb32990c00312e891 --chmod=0711 \
    https://github.com/minio/mc/releases/download/RELEASE.2025-08-13T08-35-41Z/mc.linux-amd64.RELEASE.2025-08-13T08-35-41Z \
    /usr/bin/mc
LABEL version="RELEASE.2025-08-13T08-35-41Z" \
      release="RELEASE.2025-08-13T08-35-41Z" \
      org.opencontainers.image.title="MinIO client (mirror)" \
      org.opencontainers.image.version="RELEASE.2025-08-13T08-35-41Z" \
      org.opencontainers.image.licenses="AGPL-3.0-only" \
      org.opencontainers.image.source="https://github.com/fgilde/Nextended" \
      org.opencontainers.image.description="Unmodified MinIO client RELEASE.2025-08-13T08-35-41Z from the official release binary. Upstream source: https://github.com/minio/mc/tree/RELEASE.2025-08-13T08-35-41Z"
