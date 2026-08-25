#!/usr/bin/env bash
# Build and start the PLTP web app, then open it in a browser.
#
#   ./run-web.sh                 # http://localhost:5080
#   PORT=8080 ./run-web.sh
#   NO_BROWSER=1 ./run-web.sh
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project="$root/PLTP.Web/PLTP.Web.csproj"
port="${PORT:-5080}"
url="http://localhost:$port"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "The .NET SDK is not on PATH. Install .NET 8 or newer:" >&2
  echo "  https://dotnet.microsoft.com/download" >&2
  exit 1
fi

run=(run --project "$project" -c Release --urls "$url")

if [ -z "${SKIP_BUILD:-}" ]; then
  echo "Building..."
  dotnet build "$project" -c Release --nologo -v quiet
  run+=(--no-build)
fi

if [ -z "${NO_BROWSER:-}" ]; then
  # The server takes over this shell, so the browser is opened from a subshell
  # that waits for the port to start answering.
  (
    for _ in $(seq 1 80); do
      sleep 0.3
      if (exec 3<>"/dev/tcp/127.0.0.1/$port") 2>/dev/null; then
        exec 3>&-
        if   command -v xdg-open >/dev/null 2>&1; then xdg-open "$url"
        elif command -v open     >/dev/null 2>&1; then open "$url"
        fi
        exit 0
      fi
    done
  ) &
fi

echo "PLTP on $url  (ctrl-c to stop)"
exec dotnet "${run[@]}"
