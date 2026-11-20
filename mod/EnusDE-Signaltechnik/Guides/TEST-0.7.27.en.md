# 0.7.27-P0 – direct main-signal roles

Includes all changes and outstanding gameplay checks from [0.7.26](TEST-0.7.26.en.md): simpler master-module arrangement, direct point boundaries, station signal search, bound-field refresh and painted C arrows. Keep the test save, replace the package and restart; host and clients require the same version.

## Where to select the role

- **World:** open a main signal → direction A or B → **Main signal operating role**. Enable and save the direction first. Low and high main signals use the same role selection.
- **Diagram:** right-click an assigned main signal → **Properties and assignment** → **Main signal operating role**.
- Also available in **Automation and AI → Signal roles and target automation**.

Select a role and click **Apply signal role**. Role status shows the saved value or an error. Each mast direction can have its own role.

| Role | Meaning |
| --- | --- |
| Entry signal | Controls entry from the open line into a station. |
| Intermediate signal | Controls train movements between parts of a station. |
| Exit signal | Controls departure from the station onto the open line. |
| Block signal | Main signal for a block section on the open line. |
| No special role | No special operating role assigned. |

Existing block coordination treats entry and exit roles as station boundaries. Intermediate and block roles do not mark an outer station boundary. A role alone creates no route or open-line/GWB connection. It does not change target automation, destinations, supported aspects or signal identity.

Release routes and direction locks and withdraw waiting automatic requests before changing roles. Signals serving as endpoints of an enabled open line must retain their corresponding entry/exit role; change the line configuration first if a different role is required.

## Gameplay checks

1. Set and reopen each role on an assigned main signal. Code and GUID must remain stable.
2. Set different roles on both mast directions. The opposite direction and existing target automation must remain unchanged.
3. Change a role in the world menu and check the diagram properties and automation page.

Offline checks do not replace gameplay validation. Previous AI stop tests remain outstanding.
