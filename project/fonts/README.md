# Fonts omitted

The original Claude Design prototype self-hosted six full Noto Sans TC TTFs
here (~41 MB total). They've been removed from this repo to keep it lean —
the production app loads Noto Sans TC from Google Fonts (see `index.html`),
and PrimeIcons is bundled via npm.

To run the original prototype (`project/實價通.html`) with its intended
fonts, drop the Noto Sans TC TTFs back into this folder, or point
`project/colors_and_type.css` at a CDN.
