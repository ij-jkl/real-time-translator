# Data Directory

This directory is used for local SQLite database storage during development.

## Files
- `transcriptions.db` - SQLite database file (auto-generated)
- `transcriptions.db-shm` - Shared memory file (auto-generated)
- `transcriptions.db-wal` - Write-ahead log file (auto-generated)

## Notes
- Database files are automatically created when the API Gateway starts
- This directory is excluded from version control (see .gitignore)
- In production, PostgreSQL is used instead of SQLite
