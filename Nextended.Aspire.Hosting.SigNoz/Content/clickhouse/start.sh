#!/bin/bash
# histogramQuantile (ClickHouse UDF SigNoz uses for percentiles), fetched once into the data volume
dir=/var/lib/clickhouse/user_scripts
if [ ! -x "$dir/histogramQuantile" ]; then
  mkdir -p "$dir"
  arch=$(uname -m | sed 's/aarch64/arm64/;s/x86_64/amd64/')
  if wget -qO /tmp/hq.tgz "https://github.com/SigNoz/signoz/releases/download/histogram-quantile%2Fv0.0.1/histogram-quantile_linux_${arch}.tar.gz" \
    && tar -xzf /tmp/hq.tgz -C /tmp; then
    mv /tmp/histogram-quantile "$dir/histogramQuantile"
    chmod 755 "$dir/histogramQuantile"
  else
    echo "histogramQuantile could not be downloaded; percentile panels stay empty"
  fi
fi
exec /entrypoint.sh
