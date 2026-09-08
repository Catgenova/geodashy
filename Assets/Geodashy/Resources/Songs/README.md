# Starter songs

Drop mp3, ogg or wav files in this folder and commit them. Every clip here appears in the
"Starter song" dropdown in Level Settings, and can be referenced by a Song trigger by file name
(without the extension).

Rules of thumb:
- Keep file names simple: lowercase, no spaces (`castle_march.mp3`). The name is the song id.
- ogg is the safest format across platforms; mp3 and wav also work.
- Campaign maps must use a starter song from this folder. Songs a player imports from their
  own disk live outside the project and cannot ship with the game.

Current pack (Ogg Vorbis, about quality 6, 44.1 kHz, committed as plain git blobs):
darkness_falls, final_stand, hillside_clash, le_pharaon, legends_begin, sandswept_siege,
serenity, starfall_orchestral, the_kingdom, the_old_ways, winters_hymn.

The pack is about 46 MB in total. The original 16-bit WAV masters (about 320 MB) were pushed
through Git LFS once and then replaced; they remain in the repository's LFS history. Ogg files in
this folder are deliberately excluded from LFS in .gitattributes so clones need no LFS bandwidth.
