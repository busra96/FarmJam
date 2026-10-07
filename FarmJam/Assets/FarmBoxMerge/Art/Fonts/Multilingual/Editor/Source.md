# Shared localization font source

Original font: LXGW WenKai Medium, downloaded unmodified from the project's official repository:
https://github.com/lxgw/LxgwWenKai/blob/main/fonts/TTF/LXGWWenKai-Medium.ttf

Downloaded 2026-10-08. SHA-256:
`D4BDEB38A39151D74D084CBA5090F8CB7D20BF83EEDB78C35939AE70B9F4E3F6`

Copyright and complete upstream license are retained in
`Assets/StreamingAssets/ThirdParty/LXGWWenKai-OFL.txt`.

The original font stays in this Editor-only folder. FarmBoxMergeSharedFontBuilder generates
the separately named `FarmBoxMerge Multilingual SDF` static TMP asset, validates all four
languages' current translations without fallback, and assigns that asset to every language.
Rebuild via Tools > FarmBoxMerge > Localization > Rebuild and Assign Shared Font when
new characters are added to translations.
