# OneShotLink

OneShotLink is a Telegram bot + ASP.NET Core Web API that delivers single-use, expiring access links after manual UPI payment confirmation.

## Environment variables

Required:

- TELEGRAM_BOT_TOKEN
- ADMIN_USER_IDS (comma-separated Telegram user ids)
- UPI_ID
- UPI_NAME
- BASE_URL (public HTTPS URL pointing to this service, e.g. https://yourdomain.com)

Optional:

- TOKEN_EXPIRY_MINUTES (default: 15)
- DB_PATH (default: oneshot.db in container; docker-compose sets /data/oneshot.db)

## Run with Docker

1) Create a `.env` file next to `docker-compose.yml`:

```
TELEGRAM_BOT_TOKEN=1234567890:AAF...
ADMIN_USER_IDS=123456789,987654321
UPI_ID=yourname@upi
UPI_NAME=Your Name
BASE_URL=https://yourdomain.com
TOKEN_EXPIRY_MINUTES=15
```

2) Deploy:

```bash
docker compose build
docker compose up -d
docker compose logs -f
```

## Notes

- On startup, the service applies EF Core migrations automatically and registers the Telegram webhook as `${BASE_URL}/webhook`.
- Access links are single-use. The `/access/{token}` endpoint consumes tokens atomically before serving the protected response.
