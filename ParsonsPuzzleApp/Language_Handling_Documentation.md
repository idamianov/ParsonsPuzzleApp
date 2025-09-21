# Programming Language Interpretation in Parsons Puzzle System

## Overview

The Parsons Puzzle system handles different programming languages with distinct approaches based on their syntax characteristics. The system categorizes languages into three main groups:

1. **Bracket-based languages** (C, C++, C#, Java, JavaScript)
2. **Indentation-sensitive languages** (Python)
3. **SQL-based languages** (TSQL, MySQL, PostgreSQL, plSQL)

## Language Categories

### 1. Bracket-Based Languages
**Languages:** C, C++, C#, Java, JavaScript

**Key Characteristics:**
- Use curly braces `{}` for code blocks
- Indentation is not syntactically significant
- Automatic brace generation based on indentation changes
- Comment syntax: `//`

**System Handling:**

#### Frontend (JavaScript)
```javascript
// Only add braces for bracket-based languages
const bracketLanguages = ['C', 'Cpp', 'CSharp', 'Java', 'JavaScript'];
if (!bracketLanguages.includes('@Model.Puzzle.Language.ToString()')) return;

// Dynamic brace generation based on indentation
function updateBraces() {
    // Analyzes indentation levels and automatically adds { } blocks
    // Closes braces when indentation decreases
    // Opens braces when indentation increases
}
```

#### Backend Processing
- **MultilineBlockParser**: Preprocesses source code to separate brackets from other code
- **LanguageIndentationService**: Adds braces automatically when indentation changes
- **Solution Validation**: Ignores indentation differences, focuses on content and structure

#### Key Features:
1. **Automatic Brace Generation**: When students indent blocks, the system automatically adds corresponding `{` and `}` characters
2. **Bracket Separation**: Mixed lines like `if (condition) {` are split into separate blocks
3. **Indentation Independence**: Validation ignores indentation differences between student and correct solutions

### 2. Indentation-Sensitive Languages
**Languages:** Python

**Key Characteristics:**
- Indentation is syntactically significant (defines code blocks)
- No braces for block structure
- Comment syntax: `#`
- Indentation must be preserved and validated

**System Handling:**

#### Frontend (JavaScript)
```javascript
// No automatic brace generation for Python
// Indentation buttons still work for manual adjustment
// Students must manually adjust indentation using < > buttons
```

#### Backend Processing
- **LanguageIndentationService**: Special handling for Python indentation
- **Normalization**: Converts tabs to spaces, standardizes indentation levels
- **Validation**: Compares both content AND indentation structure

#### Key Features:
1. **Indentation Preservation**: Original indentation from source code is maintained
2. **Manual Indentation Control**: Students use `<>` buttons to adjust indentation
3. **Strict Validation**: Both content and indentation must match exactly
4. **Tab-to-Space Conversion**: Standardizes indentation format

### 3. SQL-Based Languages
**Languages:** TSQL, MySQL, PostgreSQL, plSQL

**Key Characteristics:**
- No braces for block structure
- Indentation not syntactically significant
- Comment syntax: `--`
- Focus on statement ordering rather than structure

**System Handling:**

#### Frontend (JavaScript)
```javascript
// No automatic brace generation
// Indentation buttons available but not critical
// Focus on statement ordering
```

#### Backend Processing
- **LanguageIndentationService**: Ignores indentation differences
- **Content-Only Validation**: Compares statements without considering indentation
- **Statement Ordering**: Critical for correct solution validation

#### Key Features:
1. **Indentation Independence**: Validation ignores indentation differences
2. **Statement Focus**: Emphasizes correct ordering of SQL statements
3. **Content Validation**: Compares trimmed content line by line

## Language-Specific Comment Syntax

| Language | Comment Syntax | Multiline Block Markers |
|----------|----------------|-------------------------|
| C, C++, C#, Java, JavaScript | `//` | `//-->` and `//<--` |
| Python | `#` | `#-->` and `#<--` |
| TSQL, MySQL, PostgreSQL, plSQL | `--` | `-->` and `<--` |

## Code Block Processing

### Multiline Block Detection
All languages support multiline blocks using language-specific comment markers:

```csharp
// C# Example
//-->
public void Method()
{
    Console.WriteLine("Hello");
    return;
}
//<--
```

```python
# Python Example
#-->
def calculate_area(radius):
    return 3.14159 * radius ** 2
#<--
```

```sql
-- SQL Example
-->
SELECT * FROM users 
WHERE active = 1
ORDER BY name;
<--
```

### Block Parsing Logic
1. **Preprocessing**: Bracket-based languages get special bracket separation
2. **Marker Detection**: Language-specific comment patterns identify multiline blocks
3. **Content Extraction**: Preserves all lines including empty ones for formatting
4. **Indentation Removal**: Strips indentation hints to prevent student cheating

## Solution Validation Process

### 1. Language Detection
```csharp
var isBracketLanguage = BracketBasedLanguage.IsBracketBasedLanguage(puzzle.Language);
```

### 2. Expected Solution Generation
- **Bracket languages + Python**: Use original `SourceCode` to preserve structure
- **SQL languages**: Generate from `PuzzleBlocks` for better control

### 3. Validation Strategy
```csharp
if (language == Languages.Python)
{
    // Compare with indentation - both content and structure matter
    return CompareWithIndentation(normalizedStudent, normalizedCorrect);
}
else if (BracketBasedLanguage.IsBracketBasedLanguage(language))
{
    // Ignore indentation, focus on content and structure
    return CompareIgnoringIndentation(normalizedStudent, normalizedCorrect);
}
else
{
    // SQL and others - ignore indentation, focus on content
    return CompareIgnoringIndentation(normalizedStudent, normalizedCorrect);
}
```

## Frontend Language Handling

### CodeMirror Integration
```javascript
window.getCodeMirrorMode = function() {
    var language = $("#languageSelect").val();
    switch (parseInt(language)) {
        case 1: case 2: case 3: case 4: return "clike";      // C, C++, C#, Java
        case 5: return "javascript";                          // JavaScript
        case 6: return "python";                              // Python
        case 7: case 8: case 9: case 10: return "sql";       // SQL variants
        default: return "text/plain";
    }
};
```

### Indentation Controls
- **All Languages**: Manual indentation adjustment with `<>` buttons
- **Bracket Languages**: Automatic brace generation on indentation changes
- **Python**: Critical for correct syntax
- **SQL**: Optional for readability

## Key Differences Summary

| Aspect | Bracket Languages | Python | SQL Languages |
|--------|------------------|--------|---------------|
| **Block Structure** | `{}` braces | Indentation | None |
| **Indentation** | Cosmetic only | Syntactically critical | Cosmetic only |
| **Brace Generation** | Automatic | None | None |
| **Validation** | Content + structure | Content + indentation | Content only |
| **Comment Syntax** | `//` | `#` | `--` |
| **Multiline Blocks** | `//-->` / `//<--` | `#-->` / `#<--` | `-->` / `<--` |

## Implementation Notes

### Missing Language Support
**Note**: The system currently does not support YAML or other non-bracket languages beyond the defined enum. To add support for YAML or similar languages, you would need to:

1. Add the language to the `Languages` enum
2. Define appropriate comment syntax in `MultilineBlockParser`
3. Add CodeMirror mode mapping
4. Implement language-specific validation logic in `LanguageIndentationService`

### Current Language Limitations
- **YAML**: Not currently supported
- **JSON**: Not currently supported  
- **XML/HTML**: Not currently supported
- **Markdown**: Not currently supported

The system is designed to be extensible, with clear separation of concerns between language detection, parsing, and validation logic.
