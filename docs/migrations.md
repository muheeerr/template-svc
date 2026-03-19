# Database Migration Strategy

## Development

In development, `EnsureDatabaseCreated()` runs `MigrateAsync()` on startup automatically.
This is fine for local dev but **must not be used in production**.

## Production: Init Container Pattern

Never run migrations as part of application startup in production. Instead, use a Kubernetes init container:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: {{ .Release.Name }}-migrate
  annotations:
    "helm.sh/hook": pre-install,pre-upgrade
    "helm.sh/hook-weight": "-1"
    "helm.sh/hook-delete-policy": hook-succeeded
spec:
  template:
    spec:
      restartPolicy: Never
      containers:
        - name: migrate
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
          command:
            - dotnet
            - ef
            - database
            - update
            - --project
            - DA
          env:
            - name: DBHost
              valueFrom:
                secretKeyRef:
                  name: {{ .Release.Name }}-secrets
                  key: db-host
```

## Creating Migrations

```powershell
dotnet ef migrations add <Name> --project DA --startup-project projectname.Host
```

## Rollback

Generate a SQL script between two migrations to review before applying:

```powershell
dotnet ef migrations script <FromMigration> <ToMigration> --project DA --startup-project projectname.Host --output rollback.sql
```

Always test rollback scripts against a staging database before production.

## Best Practices

1. **Never auto-migrate in production** — use the init container pattern above
2. **Review generated SQL** — use `dotnet ef migrations script` to inspect before applying
3. **One migration per feature** — keep migrations small and reversible
4. **Test rollbacks** — ensure every migration can be reversed cleanly
5. **Use idempotent scripts** — pass `--idempotent` for safe re-runs
