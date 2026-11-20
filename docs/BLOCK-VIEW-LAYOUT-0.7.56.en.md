# 0.7.56-P0 – Block view editor layout fix

For Railroader 0.6.3. Every participant needs the same mod version.

The block-view editor now keeps its preview inside the native scrollable form.
The preview reserves its own 360-unit-high layout row instead of floating over
section selectors, reflection and rotation controls. Compact dropdowns replace the
large search/list controls for panel, detail and section range. Scroll the form
when using a smaller window; settings and Apply remain in the same flow.

The diagram fits the actual available viewport after layout and when the viewport
is resized. User pan and zoom are retained until its size changes. The window can
be reduced to 640 × 480 logical UI units. No changes to signal assignment, source
numbers, physical blocks, route protection or permissions are included.

Use the existing workflow: right-click an assembly → Edit block assembly view →
choose settings → Prepare view preview → optionally place → Apply this view.
Red in placement preview still means collision or a destination outside the panel;
it does not indicate a fault in the line permission.

Local compilation and full project/language/regression checks are run for this
package. Actual gameplay/UI scaling and multiplayer acceptance remain pending.
Local builds still use the available older game libraries with the existing 0.6.3
path adapter, not an original-0.6.3 DLL build.
