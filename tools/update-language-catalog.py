"""Regenerate the compiled fallback from the shipped language packs (no build-time dependency)."""
import json
from pathlib import Path
root = Path(__file__).resolve().parent.parent
pack = root / 'mod/EnusDE-Signaltechnik/Languages'
en = json.loads((pack / 'en.json').read_text(encoding='utf-8'))
de = json.loads((pack / 'de.json').read_text(encoding='utf-8'))
if en.keys() != de.keys() or any(not isinstance(v, str) or not v.strip() for pack_values in (en, de) for v in pack_values.values()):
    raise ValueError('English and German pack keys must match and both packs need nonempty string values')
quote = lambda value: json.dumps(value, ensure_ascii=False)
source = '''using System;
using System.Collections.Generic;

namespace EnusDE.Signaltechnik {
    // Generated from Languages/en.json. Keep keys identical to the German source pack.
    public static class BuiltInLanguage {
        public static Dictionary<string,string> English()=>new Dictionary<string,string>(StringComparer.Ordinal) {
'''
source += ''.join('            {' + quote(k) + ',' + quote(v) + '},\n' for k, v in sorted(en.items()))
source += '        };\n    }\n}\n'
(root / 'src/BuiltInLanguage.cs').write_text(source, encoding='utf-8', newline='\n')
