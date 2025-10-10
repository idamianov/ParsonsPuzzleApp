# Puzzle Type Categories

## Overview

This document describes the two main types of Parsons Puzzles supported by the system: Rearrangement Puzzles and Fill-in-the-Blank Puzzles. Each type has distinct characteristics, implementation details, and learning objectives.

---

## Puzzle Type Comparison

| Aspect | Rearrangement | Fill-in-the-Blank |
|--------|--------------|-------------------|
| **Student Task** | Reorder code blocks | Complete missing code parts |
| **Interaction** | Drag and drop | Type or select |
| **Focus** | Logical flow and sequence | Syntax and vocabulary |
| **Difficulty** | Understanding logic | Knowing specific constructs |
| **Code Provided** | 100% (all blocks given) | Partial (with blanks) |

---

## 1. Rearrangement Puzzles

### Definition

Students must reorder pre-written code blocks to form a correct, executable program.

### Characteristics

- **All code blocks are provided** – Nothing is missing
- **Students drag and drop to reorder** – Visual, interactive manipulation
- **Focus on logical flow and sequence** – Understanding program execution order
- **No missing code elements** – Emphasis on structure, not syntax

### Learning Objectives

- Understanding program flow and control structures
- Recognizing logical dependencies between code blocks
- Learning proper code organization
- Developing algorithmic thinking

### Example

**Given blocks (shuffled):**
```
[Block 3] return result;
[Block 1] int result = 0;
[Block 2] result = a + b;
```

**Correct solution:**
```
[Block 1] int result = 0;
[Block 2] result = a + b;
[Block 3] return result;
```

---

## 2. Fill-in-the-Blank Puzzles

### Definition

Students must complete missing parts of code by filling in blanks (placeholders) with appropriate code elements.

### Characteristics

- **Some code elements are missing** – Represented by MiniBlocks/placeholders
- **Students type or select missing parts** – Text input or dropdown selection
- **Focus on syntax and vocabulary** – Knowing the right keywords/constructs
- **Requires knowledge of specific language constructs** – Variable names, operators, keywords

### Learning Objectives

- Learning language syntax and keywords
- Understanding variable naming and types
- Practicing proper use of operators and expressions
- Reinforcing language-specific constructs

### Example

**Given code with blanks:**
```csharp
___ result = 0;
result = a ___ b;
___ result;
```

**Correct solution:**
```csharp
int result = 0;
result = a + b;
return result;
```

---

## Technical Implementation

### 1. Data Structure Differences

#### Rearrangement Puzzles

```csharp
public class PuzzleBlock
{
    public int Id { get; set; }
    public string Content { get; set; }           // Complete code content
    public int CorrectOrder { get; set; }         // Expected position
    public string BlockType { get; set; }         // "statement", "declaration", "loop", etc.
    public int IndentationLevel { get; set; }     // For proper formatting

    // For multi-line blocks
    public bool IsMultiLine { get; set; }
    public List<PuzzleBlockLine> Lines { get; set; }
}

public class RearrangementSolution
{
    public int StudentId { get; set; }
    public int PuzzleId { get; set; }
    public List<BlockPlacement> BlockOrder { get; set; }
    public bool IsCorrect { get; set; }
}

public class BlockPlacement
{
    public int BlockId { get; set; }
    public int Position { get; set; }
}
```

#### Fill-in-the-Blank Puzzles

```csharp
public class MiniBlock
{
    public int Id { get; set; }
    public string SlotName { get; set; }          // Unique identifier for the blank
    public string PlaceholderText { get; set; }   // Hint text shown to student
    public string CorrectAnswer { get; set; }     // Expected answer
    public InputType InputType { get; set; }      // "text", "dropdown", "code"
    public List<string> Options { get; set; }     // For dropdown type
    public bool IsCaseSensitive { get; set; }

    // Relationship
    public int? PuzzleBlockId { get; set; }       // For single-line blocks
    public int? PuzzleBlockLineId { get; set; }   // For multi-line blocks
}

public enum InputType
{
    Text,           // Free text input
    Dropdown,       // Select from options
    CodeEditor      // Mini code editor for complex expressions
}

public class FillInBlankSolution
{
    public int StudentId { get; set; }
    public int PuzzleId { get; set; }
    public Dictionary<string, string> SlotAnswers { get; set; }
    public bool IsCorrect { get; set; }
}
```

---

## Frontend Implementation

### 1. Rearrangement Puzzles

#### HTML Structure

```html
<div class="puzzle-workspace">
    <!-- Available blocks pool -->
    <div class="blocks-pool">
        <h3>Available Blocks</h3>
        <div class="puzzle-block" data-block-id="1" draggable="true">
            <pre><code>int result = 0;</code></pre>
        </div>
        <div class="puzzle-block" data-block-id="2" draggable="true">
            <pre><code>result = a + b;</code></pre>
        </div>
        <div class="puzzle-block" data-block-id="3" draggable="true">
            <pre><code>return result;</code></pre>
        </div>
    </div>

    <!-- Solution area -->
    <div class="solution-area">
        <h3>Your Solution</h3>
        <div class="solution-container" id="solution-drop-zone">
            <!-- Students drag blocks here -->
        </div>
    </div>
</div>
```

#### JavaScript Implementation

```javascript
// Initialize drag and drop
function initializeDragDrop() {
    // Make blocks draggable
    $('.puzzle-block').draggable({
        revert: 'invalid',
        helper: 'clone',
        cursor: 'move',
        start: function(event, ui) {
            $(this).addClass('dragging');
        },
        stop: function(event, ui) {
            $(this).removeClass('dragging');
        }
    });

    // Make solution area droppable
    $('.solution-container').sortable({
        connectWith: '.blocks-pool',
        placeholder: 'block-placeholder',
        update: function(event, ui) {
            validateSequence();
        }
    });
}

// Validate the sequence
function validateSequence() {
    var blocks = $('.solution-container .puzzle-block').map(function() {
        return $(this).data('block-id');
    }).get();

    // Send to server for validation
    $.ajax({
        url: '/api/puzzle/validate-sequence',
        method: 'POST',
        data: JSON.stringify({
            puzzleId: currentPuzzleId,
            blockOrder: blocks
        }),
        contentType: 'application/json',
        success: function(response) {
            if (response.isCorrect) {
                showSuccess();
            } else {
                showErrors(response.errors);
            }
        }
    });
}

// Visual feedback
function showErrors(errors) {
    errors.forEach(function(error) {
        var blockElement = $(`.puzzle-block[data-block-id="${error.blockId}"]`);
        blockElement.addClass('incorrect');
        blockElement.attr('title', error.message);
    });
}
```

#### CSS Styling

```css
.puzzle-block {
    background: #f5f5f5;
    border: 2px solid #ddd;
    border-radius: 4px;
    padding: 10px;
    margin: 5px 0;
    cursor: move;
    transition: all 0.3s;
}

.puzzle-block:hover {
    border-color: #007bff;
    box-shadow: 0 2px 5px rgba(0,0,0,0.1);
}

.puzzle-block.dragging {
    opacity: 0.5;
}

.puzzle-block.incorrect {
    border-color: #dc3545;
    background: #ffe6e6;
}

.block-placeholder {
    border: 2px dashed #007bff;
    background: #e7f3ff;
    height: 60px;
}
```

---

### 2. Fill-in-the-Blank Puzzles

#### HTML Structure

```html
<div class="fill-in-blank-workspace">
    <!-- Code preview with blanks -->
    <div class="code-preview">
        <pre><code><span class="mini-block-slot" data-slot="type">___</span> result = 0;
result = a <span class="mini-block-slot" data-slot="operator">___</span> b;
<span class="mini-block-slot" data-slot="keyword">___</span> result;</code></pre>
    </div>

    <!-- Input fields -->
    <div class="input-section">
        <h3>Fill in the blanks</h3>

        <div class="input-group">
            <label for="slot-type">Variable type:</label>
            <input type="text"
                   id="slot-type"
                   class="mini-block-input"
                   data-slot-name="type"
                   placeholder="Enter variable type">
        </div>

        <div class="input-group">
            <label for="slot-operator">Operator:</label>
            <select id="slot-operator"
                    class="mini-block-input"
                    data-slot-name="operator">
                <option value="">-- Select --</option>
                <option value="+">+</option>
                <option value="-">-</option>
                <option value="*">*</option>
                <option value="/">/</option>
            </select>
        </div>

        <div class="input-group">
            <label for="slot-keyword">Keyword:</label>
            <input type="text"
                   id="slot-keyword"
                   class="mini-block-input"
                   data-slot-name="keyword"
                   placeholder="Enter keyword">
        </div>
    </div>
</div>
```

#### JavaScript Implementation

```javascript
// Initialize input fields
function initializeInputFields() {
    $('.mini-block-input').on('input change', function() {
        var slotName = $(this).data('slot-name');
        var value = $(this).val();
        updateSlotContent(slotName, value);
    });
}

// Update the code preview
function updateSlotContent(slotName, content) {
    var slotElement = $(`.mini-block-slot[data-slot="${slotName}"]`);

    if (content) {
        slotElement.text(content);
        slotElement.removeClass('empty');
    } else {
        slotElement.text('___');
        slotElement.addClass('empty');
    }

    // Check if all slots are filled
    if (areAllSlotsFilled()) {
        validateContent();
    }
}

// Validate content
function validateContent() {
    var slotValues = {};

    $('.mini-block-input').each(function() {
        var slotName = $(this).data('slot-name');
        var value = $(this).val();
        slotValues[slotName] = value;
    });

    // Send to server for validation
    $.ajax({
        url: '/api/puzzle/validate-fill-in-blank',
        method: 'POST',
        data: JSON.stringify({
            puzzleId: currentPuzzleId,
            slotAnswers: slotValues
        }),
        contentType: 'application/json',
        success: function(response) {
            if (response.isCorrect) {
                showSuccess();
            } else {
                showSlotErrors(response.incorrectSlots);
            }
        }
    });
}

// Check if all slots are filled
function areAllSlotsFilled() {
    var allFilled = true;
    $('.mini-block-input').each(function() {
        if (!$(this).val()) {
            allFilled = false;
            return false; // break
        }
    });
    return allFilled;
}

// Show slot-specific errors
function showSlotErrors(incorrectSlots) {
    // Clear previous errors
    $('.mini-block-input').removeClass('incorrect');

    // Mark incorrect slots
    incorrectSlots.forEach(function(slotName) {
        var inputElement = $(`.mini-block-input[data-slot-name="${slotName}"]`);
        inputElement.addClass('incorrect');

        var slotElement = $(`.mini-block-slot[data-slot="${slotName}"]`);
        slotElement.addClass('incorrect');
    });
}
```

#### CSS Styling

```css
.code-preview {
    background: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    padding: 15px;
    margin-bottom: 20px;
    font-family: 'Courier New', monospace;
}

.mini-block-slot {
    background: #fff3cd;
    border: 1px solid #ffc107;
    border-radius: 3px;
    padding: 2px 5px;
    display: inline-block;
    min-width: 60px;
    text-align: center;
}

.mini-block-slot.empty {
    background: #e9ecef;
    border-color: #6c757d;
    color: #6c757d;
}

.mini-block-slot.incorrect {
    background: #f8d7da;
    border-color: #dc3545;
}

.input-group {
    margin-bottom: 15px;
}

.input-group label {
    display: block;
    font-weight: bold;
    margin-bottom: 5px;
}

.mini-block-input {
    width: 100%;
    padding: 8px;
    border: 2px solid #ced4da;
    border-radius: 4px;
    font-family: 'Courier New', monospace;
}

.mini-block-input:focus {
    border-color: #007bff;
    outline: none;
}

.mini-block-input.incorrect {
    border-color: #dc3545;
    background: #ffe6e6;
}
```

---

## Backend Validation

### Rearrangement Puzzles

```csharp
public class RearrangementValidator
{
    public ValidationResult ValidateSequence(int puzzleId, List<int> blockOrder)
    {
        var puzzle = _puzzleRepository.GetById(puzzleId);
        var expectedOrder = puzzle.Blocks
            .OrderBy(b => b.CorrectOrder)
            .Select(b => b.Id)
            .ToList();

        var result = new ValidationResult
        {
            IsCorrect = blockOrder.SequenceEqual(expectedOrder)
        };

        if (!result.IsCorrect)
        {
            result.Errors = FindSequenceErrors(expectedOrder, blockOrder);
        }

        return result;
    }

    private List<ValidationError> FindSequenceErrors(
        List<int> expected,
        List<int> actual)
    {
        var errors = new List<ValidationError>();

        for (int i = 0; i < actual.Count; i++)
        {
            if (i >= expected.Count || actual[i] != expected[i])
            {
                errors.Add(new ValidationError
                {
                    BlockId = actual[i],
                    Position = i,
                    Message = $"Block at position {i} is incorrect"
                });
            }
        }

        return errors;
    }
}
```

### Fill-in-the-Blank Puzzles

```csharp
public class FillInBlankValidator
{
    public ValidationResult ValidateContent(
        int puzzleId,
        Dictionary<string, string> slotAnswers)
    {
        var puzzle = _puzzleRepository.GetById(puzzleId);
        var miniBlocks = _miniBlockRepository.GetByPuzzleId(puzzleId);

        var result = new ValidationResult { IsCorrect = true };
        var incorrectSlots = new List<string>();

        foreach (var miniBlock in miniBlocks)
        {
            if (!slotAnswers.ContainsKey(miniBlock.SlotName))
            {
                result.IsCorrect = false;
                incorrectSlots.Add(miniBlock.SlotName);
                continue;
            }

            var studentAnswer = slotAnswers[miniBlock.SlotName];
            var isCorrect = ValidateAnswer(
                miniBlock,
                studentAnswer);

            if (!isCorrect)
            {
                result.IsCorrect = false;
                incorrectSlots.Add(miniBlock.SlotName);
            }
        }

        result.IncorrectSlots = incorrectSlots;
        return result;
    }

    private bool ValidateAnswer(MiniBlock miniBlock, string answer)
    {
        var correctAnswer = miniBlock.CorrectAnswer;

        // Trim whitespace
        answer = answer.Trim();
        correctAnswer = correctAnswer.Trim();

        // Check case sensitivity
        if (miniBlock.IsCaseSensitive)
        {
            return answer == correctAnswer;
        }
        else
        {
            return string.Equals(
                answer,
                correctAnswer,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}

public class ValidationResult
{
    public bool IsCorrect { get; set; }
    public List<ValidationError> Errors { get; set; }
    public List<string> IncorrectSlots { get; set; }
}

public class ValidationError
{
    public int BlockId { get; set; }
    public int Position { get; set; }
    public string Message { get; set; }
}
```

---

## Combining Both Types

A puzzle can combine both types by having:
- Multiple blocks that need to be rearranged (rearrangement aspect)
- Some blocks containing MiniBlocks/blanks (fill-in-the-blank aspect)

### Example: Hybrid Puzzle

```
[Block 1] ___ result = 0;              // Fill-in: type
[Block 2] result = a ___ b;            // Fill-in: operator
[Block 3] return result;               // Complete block

Students must:
1. Arrange blocks in correct order
2. Fill in the blanks
```

### Implementation Considerations

```csharp
public class HybridPuzzle
{
    public List<PuzzleBlock> Blocks { get; set; }
    public bool RequiresRearrangement { get; set; }
    public bool HasMiniBlocks { get; set; }

    public ValidationResult Validate(HybridSolution solution)
    {
        var result = new ValidationResult();

        // Validate sequence if rearrangement is required
        if (RequiresRearrangement)
        {
            var sequenceResult = ValidateSequence(solution.BlockOrder);
            if (!sequenceResult.IsCorrect)
            {
                result.IsCorrect = false;
                result.Errors.AddRange(sequenceResult.Errors);
            }
        }

        // Validate mini blocks if they exist
        if (HasMiniBlocks)
        {
            var miniBlockResult = ValidateContent(solution.SlotAnswers);
            if (!miniBlockResult.IsCorrect)
            {
                result.IsCorrect = false;
                result.IncorrectSlots.AddRange(miniBlockResult.IncorrectSlots);
            }
        }

        return result;
    }
}
```

---

## Best Practices

### Rearrangement Puzzles
1. Provide clear visual feedback during dragging
2. Show correct/incorrect status immediately
3. Allow students to reset and try again
4. Highlight the first incorrect block
5. Consider providing hints about dependencies

### Fill-in-the-Blank Puzzles
1. Use appropriate input types (text, dropdown, code editor)
2. Provide helpful placeholder text
3. Consider case sensitivity carefully
4. Allow partial credit for close answers
5. Show syntax highlighting in code preview

### General
1. Track time spent on each puzzle
2. Store all attempts for learning analytics
3. Provide progressive hints
4. Allow instructors to customize difficulty
5. Support both puzzle types in the same collection

---

## Related Documentation

- [Database Model](database-model.md)
- [Language Categories](language-categories.md)
- [Fill-in-the-Blank Options](fill-in-blank-options.md)
