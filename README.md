# qbit-gluetun-port-sync

An applet that keeps the qBittorrent listening port in sync with the forwarded port reported by Gluetun when using port forwarding. This is useful when using Gluetun (VPN client with port-forwarding support) together with qBittorrent so the torrent client always listens on the currently forwarded port.

Key features
- Supports all of Gluetun's authentication methods (Apikey, basic and none)
- Periodically reads the forwarded port from Gluetun.
- Updates qBittorrent's listening port automatically (via qBittorrent Web UI).
- Runs in Docker alongside Gluetun and qBittorrent.

---

Quickstart (Docker Compose)
Below is an example docker-compose.yml that demonstrates a typical setup with Gluetun, qBittorrent and qbit-gluetun-port-sync.

```yaml

services:
  gluetun:
    image: qmcgaw/gluetun
    hostname: ${HOSTNAME}
    container_name: gluetun
    # line above must be uncommented to allow external containers to connect.
    # See https://github.com/qdm12/gluetun-wiki/blob/main/setup/connect-a-container-to-gluetun.md#external-container-to-gluetun
    cap_add:
      - NET_ADMIN
    devices:
      - /dev/net/tun:/dev/net/tun
    ports:
      - 8000:8000 # default control server port, adjust as needed
      - 8085:8085 # qbit torrent port
      - 8888:8888/tcp # HTTP proxy
      - 8388:8388/tcp # Shadowsocks
      - 8388:8388/udp # Shadowsocks
    volumes:
      - /yourpath:/gluetun
    environment:
      # See https://github.com/qdm12/gluetun-wiki/tree/main/setup#setup
      - VPN_SERVICE_PROVIDER=ivpn
      - VPN_TYPE=openvpn
      # OpenVPN:
      - OPENVPN_USER=
      - OPENVPN_PASSWORD=
      - VPN_PORT_FORWARDING=on
      - PORT_FORWARD_ONLY=true
      - HTTP_CONTROL_SERVER_AUTH_DEFAULT_ROLE='{"auth":"apikey","apikey":"myapikey"}'
      # Wireguard:
      # - WIREGUARD_PRIVATE_KEY=wOEI9rqqbDwnN8/Bpp22sVz48T71vJ4fYmFWujulwUU=
      # - WIREGUARD_ADDRESSES=10.64.222.21/32
      # Timezone for accurate log times
      - TZ=
      # Server list updater
      # See https://github.com/qdm12/gluetun-wiki/blob/main/setup/servers.md#update-the-vpn-servers-list
      - UPDATER_PERIOD=
    networks:
      - vpn-net
    restart: unless-stopped

  qbittorrent:
    image: linuxserver/qbittorrent:latest
    container_name: qbittorrent
    network_mode: "service:gluetun"
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Etc/UTC
      - WEBUI_PORT=8085 # must match gluetun 
    volumes:
      - ./qbittorrent/config:/config
      - ./qbittorrent/downloads:/downloads
    restart: unless-stopped

  qbit-gluetun-port-sync:
    image: snippetsbysam/qbitgluetunportsync:latest
    container_name: qbit-gluetun-port-sync
    network_mode: "service:gluetun"
    environment:
      # qBittorrent connection settings
      - Qbittorrent__Host=localhost       # optional - localhost is default
      - Qbittorrent__Port=8085            # optional - 8085 is default
      - Qbittorrent__UseHttps=false       # optional - false is default
      - Qbittorrent__Username=admin       # required
      - Qbittorrent__Password=adminadmin  # required

      # Gluetun connection settings
      - Gluetun__Host=localhost           # optional - localhost is default
      - Gluetun__Port=8000                # optional - 8000 is default
      - Gluetun__UseHttps=false           # optional - false is default
      - Gluetun__Username=admin           # required if using basic auth
      - Gluetun__Password=adminadmin      # required if using basic auth
      - Gluetun__ApiKey=yourapikey        # required if using api key auth

      # Timings are optional - default values are below
      - Timings__InitialDelaySeconds=5
      - Timings__CheckIntervalSeconds=300
      - Timings__ErrorIntervalSeconds=10
    restart: "unless-stopped"

networks:
  vpn-net:
    name: vpn-net
```


Build Docker image:

```bash
# from repo root
docker docker build -f QbitGluetunPortSync.Service/Dockerfile -t snippetsbysam/qbitgluetunportsync:latest
```


Contact / Support
For issues or questions, please open an issue in the repository: [https://github.com/snippetsBySam/qbitgluetunportsync/issues](https://github.com/snippetsBySam/qbitgluetunportsync/issues)
