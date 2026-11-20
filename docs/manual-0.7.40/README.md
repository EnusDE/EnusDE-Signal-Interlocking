# Standalone illustrated public-release manuals

This directory authors the German and English manuals for **public 0.7.40-P0**,
Railroader 0.6.2. It is documentation only. Prepared mod ZIPs remain immutable.

`content.py` is the bilingual text source; `build.py` builds PDFs, self-contained
HTML, editable Markdown, structured JSON, vector assets and an evidence matrix.
`extras.py` creates the bilingual screenshot agendas and package readme.

Run from the repository root:

```sh
python3 docs/manual-0.7.40/build.py
python3 docs/manual-0.7.40/extras.py
python3 docs/manual-0.7.40/validate.py
```

The build requires Python, ReportLab, Pillow, PyMuPDF for validation and DejaVu
Sans fonts. Output is `build/manuals/0.7.40-r1/`. It checks the exact public
mod archive SHA-256 before generation. The archived guide image must already be
available under `build/manuals/evidence-0.7.40/EnusDE-Signaltechnik/Guides/images/`.
This directory comes from extracting the original mod ZIP, without editing it.

Review the archived guides referenced in `FEATURE-COVERAGE.csv`, not the current
live source or newer test guides. Later instructions within the archived package
supersede older incremental guide text. Reconcile contradictions before adding
new claims. The original 0.7.40 WIP tutorial was used as supplementary evidence.

Edit both languages together in `content.py` and regenerate. Markdown files are
convenient review copies; editing only generated Markdown does not change PDFs.
Use a new documentation edition/output directory for later published revisions.
All explanations and original diagrams are version-specific, and screenshot
agendas intentionally request real captures rather than fabricated UI images.

Keep tests appropriate: validate document structure, links, text, sources and
page layout, then visually inspect representative pages. Runtime mod tests are
not required for these documentation-only changes. Before packaging, exclude
QA renders and archive evidence; never include the runtime DLL or diagnostics.
