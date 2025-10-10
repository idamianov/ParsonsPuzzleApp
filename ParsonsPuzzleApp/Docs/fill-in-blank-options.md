# Fill-in-the-Blank Puzzle Options

## Overview

This document explores various options and strategies for implementing fill-in-the-blank puzzles in the Parsons Puzzle application. It covers different input methods, validation strategies, and user experience considerations to create effective learning experiences.

---

## Input Method Options

### 1. Free Text Input

**Description:** Students type the answer directly into a text field.

#### Advantages
- Maximum flexibility for student responses
- Tests recall rather than recognition
- Allows for various correct answers
- Natural for code expressions

#### Disadvantages
- Requires complex validation (typos, whitespace, casing)
- Higher cognitive load
- May frustrate beginners
- Difficult to provide immediate feedback

#### Implementation

```html
<input type="text"
       class="mini-block-input"
       data-slot-name="variable"
       placeholder="Enter variable name"
       autocomplete="off">
```

```csharp
public class FreeTextMiniBlock : MiniBlock
{
    public bool IsCaseSensitive { get; set; }
    public bool IgnoreWhitespace { get; set; }
    public List<string> AcceptableAnswers { get; set; } // Multiple correct answers
    public string RegexPattern { get; set; } // For pattern matching
}
```

#### Best Use Cases
- Variable names
- Simple expressions
- Advanced students who should recall syntax
- Keywords in context

---

### 2. Dropdown Selection

**Description:** Students select the answer from a predefined list of options.

#### Advantages
- Prevents typos and syntax errors
- Easier for beginners
- Clear set of options
- Immediate validation possible

#### Disadvantages
- Tests recognition, not recall
- Limited to predefined options
- Can be too easy if options are obvious
- May include too many distractors

#### Implementation

```html
<select class="mini-block-input" data-slot-name="operator">
    <option value="">-- Select operator --</option>
    <option value="+">+ (addition)</option>
    <option value="-">- (subtraction)</option>
    <option value="*">* (multiplication)</option>
    <option value="/">/  (division)</option>
</select>
```

```csharp
public class DropdownMiniBlock : MiniBlock
{
    public List<DropdownOption> Options { get; set; }
    public bool ShowHints { get; set; } // Show descriptions with options
}

public class DropdownOption
{
    public string Value { get; set; }
    public string DisplayText { get; set; }
    public string Description { get; set; } // Optional hint
}
```

#### Best Use Cases
- Operators (+, -, *, /)
- Keywords (if, while, for, return)
- Data types (int, string, bool)
- Fixed set of valid options

---

### 3. Code Editor (Monaco/CodeMirror)

**Description:** A mini code editor for multi-line or complex code expressions.

#### Advantages
- Syntax highlighting
- Auto-completion
- Handles complex expressions
- Professional coding experience

#### Disadvantages
- More complex implementation
- May be overkill for simple blanks
- Requires more screen space
- Slower for quick answers

#### Implementation

```html
<div class="mini-code-editor"
     data-slot-name="expression"
     data-language="csharp"></div>
```

```javascript
// Initialize Monaco editor
function initializeMiniCodeEditor(element) {
    var editor = monaco.editor.create(element, {
        value: '',
        language: element.dataset.language,
        minimap: { enabled: false },
        lineNumbers: 'off',
        scrollBeyondLastLine: false,
        automaticLayout: true,
        theme: 'vs-light'
    });

    editor.onDidChangeModelContent(function() {
        var slotName = element.dataset.slotName;
        var value = editor.getValue();
        updateSlotContent(slotName, value);
    });

    return editor;
}
```

```csharp
public class CodeEditorMiniBlock : MiniBlock
{
    public string Language { get; set; }
    public bool EnableAutoComplete { get; set; }
    public bool EnableSyntaxCheck { get; set; }
    public int MaxLines { get; set; }
}
```

#### Best Use Cases
- Function bodies
- Complex expressions
- Multi-line statements
- Lambda expressions

---

### 4. Drag-and-Drop Tokens

**Description:** Students drag predefined code tokens into blank spaces.

#### Advantages
- Visual and interactive
- Prevents syntax errors completely
- Fun and engaging
- Good for mobile devices

#### Disadvantages
- Limited to predefined tokens
- Can be too restrictive
- More complex UI implementation
- May not scale to many options

#### Implementation

```html
<div class="token-pool">
    <span class="code-token" draggable="true" data-value="int">int</span>
    <span class="code-token" draggable="true" data-value="string">string</span>
    <span class="code-token" draggable="true" data-value="bool">bool</span>
</div>

<div class="code-line">
    <span class="drop-zone" data-slot-name="type">___</span> result = 0;
</div>
```

```javascript
// Make tokens draggable
$('.code-token').draggable({
    revert: 'invalid',
    helper: 'clone'
});

// Make drop zones accept tokens
$('.drop-zone').droppable({
    accept: '.code-token',
    drop: function(event, ui) {
        var slotName = $(this).data('slot-name');
        var value = ui.draggable.data('value');
        $(this).text(value);
        updateSlotContent(slotName, value);
    }
});
```

```csharp
public class DragDropMiniBlock : MiniBlock
{
    public List<string> AvailableTokens { get; set; }
    public bool AllowReuse { get; set; } // Can same token be used multiple times?
}
```

#### Best Use Cases
- Data types
- Keywords
- Simple operators
- Beginner-level puzzles

---

### 5. Auto-Complete Input

**Description:** Text input with auto-complete suggestions as students type.

#### Advantages
- Combines flexibility with guidance
- Helps students learn valid options
- Reduces typos
- Professional feel

#### Disadvantages
- Requires JavaScript library
- May give away answers too easily
- Needs careful tuning of suggestions
- Can be distracting

#### Implementation

```html
<input type="text"
       class="autocomplete-input"
       data-slot-name="method"
       placeholder="Enter method name">
```

```javascript
// Initialize auto-complete
$('.autocomplete-input').autocomplete({
    source: function(request, response) {
        var slotName = this.element.data('slot-name');
        var suggestions = getSuggestionsForSlot(slotName, request.term);
        response(suggestions);
    },
    minLength: 1,
    select: function(event, ui) {
        var slotName = $(this).data('slot-name');
        updateSlotContent(slotName, ui.item.value);
    }
});

function getSuggestionsForSlot(slotName, term) {
    // Get language-specific suggestions
    var language = getCurrentLanguage();
    var keywords = getKeywordsForLanguage(language);

    return keywords.filter(function(keyword) {
        return keyword.toLowerCase().startsWith(term.toLowerCase());
    });
}
```

```csharp
public class AutoCompleteMiniBlock : MiniBlock
{
    public List<string> Suggestions { get; set; }
    public int MinCharactersForSuggestions { get; set; }
    public bool FuzzyMatch { get; set; }
}
```

#### Best Use Cases
- Method names
- Variable names
- Class names
- Language keywords

---

## Validation Strategies

### 1. Exact Match

**Description:** Student answer must exactly match the expected answer.

```csharp
public bool ValidateExactMatch(string answer, string expected)
{
    return answer.Trim() == expected.Trim();
}
```

**Use when:**
- Syntax is critical
- Only one correct answer
- Teaching specific keywords

---

### 2. Case-Insensitive Match

**Description:** Ignore case differences in validation.

```csharp
public bool ValidateCaseInsensitive(string answer, string expected)
{
    return string.Equals(
        answer.Trim(),
        expected.Trim(),
        StringComparison.OrdinalIgnoreCase);
}
```

**Use when:**
- Language is case-insensitive
- Testing conceptual understanding
- Variable naming practice

---

### 3. Multiple Acceptable Answers

**Description:** Accept any answer from a list of correct answers.

```csharp
public bool ValidateMultipleAnswers(string answer, List<string> acceptableAnswers)
{
    answer = answer.Trim().ToLower();
    return acceptableAnswers.Any(a =>
        a.Trim().ToLower() == answer);
}
```

**Use when:**
- Multiple valid solutions exist
- Synonyms are acceptable
- Different coding styles are valid

**Example:**
```csharp
// Acceptable answers for a loop variable
var acceptableAnswers = new List<string> { "i", "index", "counter", "idx" };
```

---

### 4. Pattern Matching (Regex)

**Description:** Use regular expressions to validate answer format.

```csharp
public bool ValidatePattern(string answer, string regexPattern)
{
    return Regex.IsMatch(answer.Trim(), regexPattern);
}
```

**Use when:**
- Format matters more than exact content
- Teaching naming conventions
- Accepting variable expressions

**Examples:**
```csharp
// Variable name must start with letter, contain alphanumeric
var variablePattern = @"^[a-zA-Z][a-zA-Z0-9]*$";

// Number literal (integer or decimal)
var numberPattern = @"^\d+(\.\d+)?$";

// Simple arithmetic expression
var expressionPattern = @"^\d+\s*[\+\-\*\/]\s*\d+$";
```

---

### 5. Semantic Validation

**Description:** Compile/execute the code to check if it's semantically correct.

```csharp
public bool ValidateSemantic(string answer, string context)
{
    try
    {
        var fullCode = context.Replace("___", answer);
        var result = CompileAndCheck(fullCode);
        return result.IsValid;
    }
    catch
    {
        return false;
    }
}
```

**Use when:**
- Teaching problem-solving
- Multiple solutions are valid
- Want to accept creative answers

**Considerations:**
- Performance overhead
- Security concerns (code execution)
- Complex error handling needed

---

### 6. Partial Credit

**Description:** Award points for partially correct answers.

```csharp
public class PartialCreditValidator
{
    public ValidationResult Validate(string answer, string expected)
    {
        var result = new ValidationResult();

        // Exact match = 100%
        if (answer == expected)
        {
            result.Score = 1.0;
            result.IsCorrect = true;
            return result;
        }

        // Case mismatch = 80%
        if (answer.ToLower() == expected.ToLower())
        {
            result.Score = 0.8;
            result.Feedback = "Correct, but check the casing";
            return result;
        }

        // Close match (Levenshtein distance) = 50-70%
        var distance = LevenshteinDistance(answer, expected);
        if (distance <= 2)
        {
            result.Score = 0.5 + (0.2 * (2 - distance));
            result.Feedback = "Close! Check your spelling";
            return result;
        }

        result.Score = 0.0;
        result.IsCorrect = false;
        return result;
    }
}
```

**Use when:**
- Want to encourage students
- Typos shouldn't fully penalize
- Formative assessment

---

## User Experience Considerations

### 1. Inline vs Separate Input

#### Inline Input
**Blanks appear directly in the code**

```html
<pre><code>int <input type="text" data-slot="var"> = 0;</code></pre>
```

**Advantages:**
- Contextual
- Space-efficient
- Natural flow

**Disadvantages:**
- May break code formatting
- Harder to implement
- Mobile-unfriendly

---

#### Separate Input Section
**Input fields below the code preview**

```html
<div class="code-preview">
    <pre><code>int ___ = 0;</code></pre>
</div>
<div class="inputs">
    <input type="text" data-slot="var">
</div>
```

**Advantages:**
- Cleaner code display
- Easier to implement
- Better for mobile

**Disadvantages:**
- Disconnect from context
- More scrolling
- Less intuitive

---

### 2. Real-Time vs Submit-Based Validation

#### Real-Time Validation

```javascript
$('.mini-block-input').on('input', function() {
    validateAnswer($(this));
});
```

**Advantages:**
- Immediate feedback
- Catches errors early
- Engaging

**Disadvantages:**
- Can be frustrating
- May not allow thinking time
- Performance concerns

---

#### Submit-Based Validation

```javascript
$('#submit-button').on('click', function() {
    validateAllAnswers();
});
```

**Advantages:**
- Students can think
- Less pressure
- Better for complex answers

**Disadvantages:**
- Delayed feedback
- May waste time on wrong path
- Less engaging

---

### 3. Hint System

#### Progressive Hints

```csharp
public class HintSystem
{
    public List<string> Hints { get; set; }
    private int currentHintIndex = 0;

    public string GetNextHint()
    {
        if (currentHintIndex < Hints.Count)
        {
            return Hints[currentHintIndex++];
        }
        return null;
    }
}

// Hints for a variable type question
var hints = new List<string>
{
    "Think about what type of data you're storing",
    "This variable will store whole numbers",
    "The type is a 32-bit integer",
    "The answer is 'int'"
};
```

#### Implementation

```html
<button class="hint-button" data-slot="type">Show Hint</button>
<div class="hint-display" data-slot="type" style="display:none;"></div>
```

```javascript
$('.hint-button').on('click', function() {
    var slotName = $(this).data('slot');
    $.get('/api/hints/next/' + slotName, function(hint) {
        $('.hint-display[data-slot="' + slotName + '"]')
            .text(hint)
            .fadeIn();
    });
});
```

---

### 4. Visual Feedback

#### Color Coding

```css
/* Correct answer */
.mini-block-input.correct {
    border-color: #28a745;
    background: #d4edda;
}

/* Incorrect answer */
.mini-block-input.incorrect {
    border-color: #dc3545;
    background: #f8d7da;
}

/* Partial credit */
.mini-block-input.partial {
    border-color: #ffc107;
    background: #fff3cd;
}

/* Unanswered */
.mini-block-input.empty {
    border-color: #6c757d;
    background: #e9ecef;
}
```

#### Icons and Animations

```html
<div class="input-with-feedback">
    <input type="text" class="mini-block-input">
    <span class="feedback-icon">
        <i class="fas fa-check correct-icon"></i>
        <i class="fas fa-times incorrect-icon"></i>
    </span>
</div>
```

```javascript
function showFeedback(inputElement, isCorrect) {
    var feedbackIcon = inputElement.siblings('.feedback-icon');

    if (isCorrect) {
        feedbackIcon.find('.correct-icon').fadeIn();
        inputElement.addClass('correct');
    } else {
        feedbackIcon.find('.incorrect-icon').shake();
        inputElement.addClass('incorrect');
    }
}
```

---

## Advanced Features

### 1. Context-Aware Suggestions

Provide suggestions based on surrounding code context.

```csharp
public class ContextAwareSuggestions
{
    public List<string> GetSuggestions(MiniBlock miniBlock, string surroundingCode)
    {
        var language = miniBlock.Language;
        var context = AnalyzeContext(surroundingCode);

        // If in a conditional statement, suggest boolean expressions
        if (context.IsConditionalContext)
        {
            return GetBooleanExpressionSuggestions(language);
        }

        // If declaring a variable, suggest appropriate types
        if (context.IsDeclarationContext)
        {
            return GetDataTypeSuggestions(language);
        }

        // Default suggestions
        return GetGeneralSuggestions(language);
    }
}
```

---

### 2. Intelligent Error Messages

Provide specific, helpful error messages.

```csharp
public class IntelligentErrorMessages
{
    public string GetErrorMessage(string answer, string expected)
    {
        // Case error
        if (answer.ToLower() == expected.ToLower())
        {
            return $"Almost! Check the casing. Expected '{expected}'";
        }

        // Missing semicolon
        if (answer + ";" == expected)
        {
            return "Don't forget the semicolon!";
        }

        // Extra whitespace
        if (answer.Replace(" ", "") == expected.Replace(" ", ""))
        {
            return "Check your spacing";
        }

        // Similar word (Levenshtein distance)
        if (LevenshteinDistance(answer, expected) <= 2)
        {
            return $"Close! Check the spelling. Expected '{expected}'";
        }

        // Wrong type of answer
        if (IsOperator(answer) && IsDataType(expected))
        {
            return "That's an operator, but we need a data type here";
        }

        return "Incorrect. Try again!";
    }
}
```

---

### 3. Adaptive Difficulty

Adjust difficulty based on student performance.

```csharp
public class AdaptiveDifficulty
{
    public InputType GetInputType(Student student, MiniBlock miniBlock)
    {
        var performance = student.GetPerformanceForTopic(miniBlock.Topic);

        // Struggling students get dropdowns
        if (performance < 0.5)
        {
            return InputType.Dropdown;
        }

        // Average students get autocomplete
        if (performance < 0.8)
        {
            return InputType.AutoComplete;
        }

        // Advanced students get free text
        return InputType.FreeText;
    }
}
```

---

## Recommendations

### For Beginners
1. Use **dropdown** for keywords and operators
2. Use **drag-and-drop tokens** for data types
3. Provide **progressive hints**
4. Use **real-time validation** with encouraging messages
5. Accept **multiple correct answers** when appropriate

### For Intermediate Students
1. Use **autocomplete** for common constructs
2. Use **free text** for variables and simple expressions
3. Use **pattern matching** validation
4. Provide hints only on request
5. Use **submit-based validation** to encourage thinking

### For Advanced Students
1. Use **free text** for everything
2. Use **code editor** for complex expressions
3. Use **semantic validation** to accept creative solutions
4. Minimal hints, focus on problem-solving
5. Provide **detailed feedback** on errors

### General Best Practices
1. **Match input type to content**: Use dropdown for fixed options, text for variables
2. **Provide clear placeholders**: Help students understand what's expected
3. **Balance difficulty**: Not too easy (giving away answers), not too hard (frustrating)
4. **Test with real students**: Gather feedback and iterate
5. **Track analytics**: Monitor which blanks are most difficult
6. **Accessibility**: Ensure keyboard navigation and screen reader support

---

## Related Documentation

- [Database Model](database-model.md)
- [Language Categories](language-categories.md)
- [Puzzle Types](puzzle-types.md)
