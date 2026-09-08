# Starter songs

Drop mp3, ogg or wav files in this folder and commit them. Every clip here appears in the
"Starter song" dropdown in Level Settings, and can be referenced by a Song trigger by file name
(without the extension).

Rules of thumb:
- Keep file names simple: lowercase, no spaces (`castle_march.mp3`). The name is the song id.
- ogg is the safest format across platforms; mp3 and wav also work.
- Campaign maps must use a starter song from this folder. Songs a player imports from their
  own disk live outside the project and cannot ship with the game.

Current pack (16-bit 44.1 kHz WAV, stored through Git LFS):
darkness_falls, final_stand, hillside_clash, le_pharaon, legends_begin, sandswept_siege,
serenity, starfall_orchestral, the_kingdom, the_old_ways, winters_hymn.

Note on size: the pack is about 320 MB, and GitHub's free LFS tier allows 1 GB of storage and
1 GB of download bandwidth per month, so every fresh clone spends a third of that. Converting the
pack to ogg (quality 6 or so) would shrink it to roughly 50 MB with no audible loss in-game, since
Unity re-encodes audio to Vorbis in builds anyway.
