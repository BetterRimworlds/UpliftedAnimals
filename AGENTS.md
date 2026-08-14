# Project agent notes (Uplifted Animals)

## Git signing

This repo signs commits with GPG (`commit.gpgsign=true`, key `D8EA6E4D5952159D77592BB4EEB6CE72F441EC41`).

Agent shells have no TTY (`gpg: cannot open '/dev/tty'`). `gpg --batch` cannot ask for a passphrase and will fail.

Do **not** fall back to `--no-gpg-sign` unless the maintainer asks. That leaves an unsigned commit that has to be amended.

This desktop has `DISPLAY=:0` and `pinentry-gnome3`. Sign **without** `--batch` so pinentry can open a GUI prompt on the local session:

```bash
# unlock / prove the agent can sign
echo test | gpg --pinentry-mode ask --clearsign -u D8EA6E4D5952159D77592BB4EEB6CE72F441EC41

# then commit or re-sign
git commit -S
git commit --amend --no-edit -S
```

The maintainer approves the pinentry dialog. After that, gpg-agent caches the passphrase and later signed commits work until the cache expires.

If signing still fails, leave the changes staged and ask the maintainer to unlock gpg-agent. Do not invent a workaround that drops the signature.
