# HOWTO: Create a terminal-session capture of application

https://docs.asciinema.org/manual/cli/quick-start/

## Create capture

```
sudo pacman -S asciinema
asciinema rec -c "git-rstatus --exclude tmux,archive --no-fetch linux --pull ~/repo ~/scripts --abs" git-rstatus.cast
asciinema play git-rstatus.cast
```

## Convert to animated .gif

> Github does not support the embedded `https://asciinema.org` resources

```
~/apps/agg-x86_64-unknown-linux-gnu ~/temp/git-rstatus.cast ~/temp/git-rstatus.gif
```
