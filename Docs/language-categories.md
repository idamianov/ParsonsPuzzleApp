# Language Categories in the System

## Overview

This document describes how the Parsons Puzzle application handles different programming language categories, particularly the distinctions between bracket-based and non-bracket languages after the refactoring of the Language entity.

---

## Language Categories

The system classifies programming languages into three main categories based on their syntactic characteristics:

### 1. Bracket-based Languages

**Category**: `LanguageCategory.Bracket`

**Examples:**
- C#
- Java
- JavaScript
- C++
- C

**Characteristics:**
- Use curly braces `{}` to define code blocks
- Semicolon-terminated statements
- Explicit block delimiters
- Clear structural boundaries
- Nesting through bracket matching

**Example Code:**
```csharp
if (condition) {
    statement1;
    statement2;
}
```

---

### 2. Non-bracket Languages (Indentation-based)

**Category**: `LanguageCategory.Indentation`

**Examples:**
- Python
- YAML
- Ruby (optional brackets)
- Bash

**Characteristics:**
- Indentation-based block structure
- No explicit block delimiters
- Whitespace-sensitive syntax
- Natural language-like structure
- Nesting through indentation depth

**Example Code:**
```python
if condition:
    statement1
    statement2
```

---

### 3. SQL-based Languages

**Category**: `LanguageCategory.SQL`

**Examples:**
- SQL
- T-SQL
- PL/SQL
- PostgreSQL

**Characteristics:**
- Declarative syntax
- Statement-based structure
- Specific query language constructs
- Different parsing rules than procedural languages

**Example Code:**
```sql
SELECT column1, column2
FROM table_name
WHERE condition;
```

---

## Technical Implementation

### Language Entity Structure

```csharp
public class Language
{
    public int Id { get; set; }
    public string Name { get; set; }
    public LanguageCategory Category { get; set; }

    // Helper properties
    public bool IsBracketBased => Category == LanguageCategory.Bracket;
    public bool IsIndentationSensitive => Category == LanguageCategory.Indentation;
    public bool IsSqlBased => Category == LanguageCategory.SQL;
}
```

### Category Classification Enum

```csharp
public enum LanguageCategory
{
    Bracket,        // C-style languages with explicit delimiters
    Indentation,    // Python-style indentation-sensitive languages
    SQL             // Database query languages
}
```

---

## Parsing Differences

### Bracket-based Languages

#### Block Detection
- Relies on matching `{` and `}` characters
- Uses bracket counting to determine nesting depth
- Can have nested blocks at any level

```csharp
public class BracketParser
{
    public List<CodeBlock> ParseBlocks(string code)
    {
        int bracketDepth = 0;
        // ... parse using bracket matching
    }
}
```

#### Statement Separation
- Uses semicolons `;` as statement delimiters
- Multiple statements can exist on one line
- Empty statements are valid

#### Nesting
- Handles nested blocks through bracket counting
- Maintains a stack of open brackets
- Validates matching opening and closing brackets

#### Error Handling
- Syntax errors often related to:
  - Unmatched brackets
  - Missing semicolons
  - Incorrect bracket placement

---

### Non-bracket Languages

#### Block Detection
- Analyzes indentation levels (spaces or tabs)
- Compares current line indentation to previous line
- Uses indentation decrease to detect block end

```csharp
public class IndentationParser
{
    public List<CodeBlock> ParseBlocks(string code)
    {
        int currentIndent = 0;
        // ... parse using indentation levels
    }
}
```

#### Statement Separation
- Line-based parsing (each line is typically a statement)
- Multi-line statements use explicit continuation characters
- No semicolons required (except for optional multi-statement lines)

#### Nesting
- Indentation depth determines block hierarchy
- Each indentation level represents a nested block
- Consistent indentation is critical

#### Error Handling
- Indentation inconsistencies cause parsing issues:
  - Mixed tabs and spaces
  - Unexpected indentation levels
  - Inconsistent indentation within a block

---

## System Behavior

### 1. Code Block Generation

#### Bracket Languages
```csharp
public string GenerateBlock(CodeBlock block)
{
    return $"{{\n{block.Content}\n}}";
}
```

**Output:**
```csharp
{
    statement1;
    statement2;
}
```

#### Indentation Languages
```csharp
public string GenerateBlock(CodeBlock block, int indentLevel)
{
    var indent = new string(' ', indentLevel * 4);
    return $"{indent}{block.Content}";
}
```

**Output:**
```python
    statement1
    statement2
```

---

### 2. Validation Logic

#### Bracket Languages
```csharp
public bool ValidateSyntax(string code)
{
    // Validate bracket matching
    int bracketCount = 0;
    foreach (char c in code)
    {
        if (c == '{') bracketCount++;
        if (c == '}') bracketCount--;
        if (bracketCount < 0) return false;
    }
    return bracketCount == 0;
}
```

#### Indentation Languages
```csharp
public bool ValidateIndentation(string code)
{
    // Validate indentation consistency
    var lines = code.Split('\n');
    int previousIndent = 0;

    foreach (var line in lines)
    {
        int currentIndent = GetIndentLevel(line);
        // Check for valid indentation changes
        if (currentIndent > previousIndent + 4)
            return false; // Invalid indent jump
        previousIndent = currentIndent;
    }
    return true;
}
```

---

### 3. Student Experience

#### Bracket Languages

**Learning Focus:**
- Understanding block structure and scope
- Proper bracket placement
- Statement termination with semicolons

**Common Mistakes:**
- Missing or extra brackets
- Forgotten semicolons
- Incorrect nesting

**Puzzle Design:**
- Blocks can be visually distinct with brackets
- Clear beginning and end markers
- Structure is explicit

---

#### Indentation Languages

**Learning Focus:**
- Understanding whitespace significance
- Maintaining consistent indentation
- Recognizing block boundaries through indentation

**Common Mistakes:**
- Inconsistent indentation
- Mixed tabs and spaces
- Incorrect indentation depth

**Puzzle Design:**
- Must preserve indentation in puzzle blocks
- Visual cues for indentation levels
- More careful handling of whitespace

---

## Frontend Considerations

### Display Differences

#### Bracket Languages
```html
<div class="code-block bracket-language">
    <pre><code>if (condition) {
    statement;
}</code></pre>
</div>
```

#### Indentation Languages
```html
<div class="code-block indentation-language">
    <pre><code>if condition:
    statement</code></pre>
</div>
```

**CSS Considerations:**
```css
.indentation-language {
    /* Preserve whitespace */
    white-space: pre;
    /* Use monospace font for clear indentation */
    font-family: 'Courier New', monospace;
}
```

---

## Best Practices

### For Bracket Languages
1. Always validate bracket matching before accepting puzzles
2. Provide syntax highlighting for brackets
3. Show bracket pairs on hover
4. Auto-format code with consistent bracket placement

### For Indentation Languages
1. Enforce consistent indentation (spaces or tabs, not mixed)
2. Visualize indentation levels (guides or background shading)
3. Preserve exact whitespace in puzzle blocks
4. Validate indentation on puzzle creation and solution submission

### For All Languages
1. Use the `Language.Category` property to determine parsing strategy
2. Apply language-specific validation rules
3. Provide appropriate error messages based on language type
4. Design puzzles that respect language conventions

---

## Migration Notes

When adding a new language to the system:

1. Determine the appropriate `LanguageCategory`
2. Configure parsing rules based on category
3. Test with sample code to ensure correct parsing
4. Update validation logic if needed
5. Consider edge cases specific to the language

---

## Related Documentation

- [Database Model](database-model.md)
- [Puzzle Types](puzzle-types.md)
- [Fill-in-the-Blank Options](fill-in-blank-options.md)
