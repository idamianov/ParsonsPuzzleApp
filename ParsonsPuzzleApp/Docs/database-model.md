# Database Model for Solutions Storage

## Overview

This document describes the evolution of the database model used for storing puzzles and student solutions, highlighting the improvements from the old structure to the new normalized approach.

---

## Old Structure

### Design

In the previous implementation:

- A **Puzzle** contained a collection of **PuzzleBlocks** and **MiniBlocks**
- **MiniBlocks** were attached globally at the puzzle level, not tied directly to a specific block or line
- Student attempts were stored in the `StudentAttempt` table where the solution was saved as:
  - **StudentArrangement** – JSON data containing the student's block arrangement

### Limitations

The old structure had several significant limitations:

1. **Non-normalized JSON Storage**: The JSON storage for arrangements was not normalized, making it difficult to:
   - Query and analyze solutions efficiently
   - Generate reports on student performance
   - Identify common mistake patterns
   - Perform data migrations

2. **Ambiguous MiniBlock Association**: MiniBlocks not being tied to specific blocks/lines caused:
   - Unclear placeholder placement
   - Difficulty in validating student answers
   - Ambiguity in determining which part of the code the MiniBlock belongs to

---

## New Normalized Structure

### Design Principles

The new structure follows database normalization principles and provides clear relationships between entities.

### Entity Structure

#### Puzzle
- Contains only **PuzzleBlocks** (no longer stores MiniBlocks globally)
- Each PuzzleBlock can be either single-line or multi-line

#### PuzzleBlock
- Represents a code block that can be:
  - **Single-line block**: Contains a single line of code
  - **Multi-line block**: Contains multiple lines of code

**If single-line:**
- May contain its own **MiniBlocks** directly attached to the block

**If multi-line:**
- Contains a list of **PuzzleBlockLines**

#### PuzzleBlockLine
- Represents a single line of code inside a multi-line block
- Each line can have its own **MiniBlocks**

#### MiniBlocks
- No longer "global" under Puzzle
- Directly tied to the exact block or line where the placeholder appears
- Clear ownership and relationship to specific code elements

### Entity Relationship Diagram

```
Puzzle
  └─ PuzzleBlocks (1:N)
      ├─ MiniBlocks (1:N) [for single-line blocks]
      └─ PuzzleBlockLines (1:N) [for multi-line blocks]
          └─ MiniBlocks (1:N)
```

---

## Solution Storage

### Old Approach

Previously, solutions were stored as JSON in the `StudentArrangement` field:

```json
{
  "blocks": [
    {"id": 1, "order": 0},
    {"id": 3, "order": 1},
    {"id": 2, "order": 2}
  ],
  "miniBlocks": {
    "slot1": "answer1",
    "slot2": "answer2"
  }
}
```

**Problems:**
- Difficult to query
- No referential integrity
- Hard to maintain
- Performance issues with large datasets

### New Normalized Approach

Instead of storing only JSON, solutions are now saved in a normalized way:

- Store the actual placement of **blocks**
- Store the actual placement of **lines** within multi-line blocks
- Store **mini-blocks** chosen by the student with direct references

**Benefits:**
- Full relational database capabilities
- Easy querying and reporting
- Data integrity through foreign keys
- Better performance for analytics
- Easier to extend and maintain

### Example Schema

```csharp
public class StudentSolution
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int PuzzleId { get; set; }
    public DateTime SubmittedAt { get; set; }

    // Navigation properties
    public List<StudentBlockPlacement> BlockPlacements { get; set; }
    public List<StudentMiniBlockAnswer> MiniBlockAnswers { get; set; }
}

public class StudentBlockPlacement
{
    public int Id { get; set; }
    public int StudentSolutionId { get; set; }
    public int PuzzleBlockId { get; set; }
    public int Order { get; set; }

    // For multi-line blocks
    public List<StudentLinePlacement> LinePlacements { get; set; }
}

public class StudentLinePlacement
{
    public int Id { get; set; }
    public int StudentBlockPlacementId { get; set; }
    public int PuzzleBlockLineId { get; set; }
    public int Order { get; set; }
}

public class StudentMiniBlockAnswer
{
    public int Id { get; set; }
    public int StudentSolutionId { get; set; }
    public int MiniBlockId { get; set; }
    public string Answer { get; set; }
}
```

---

## Migration Strategy

When migrating from the old structure to the new:

1. **Parse existing JSON data** from `StudentArrangement`
2. **Create normalized records** in new tables
3. **Maintain backward compatibility** during transition period
4. **Validate migrated data** against original JSON
5. **Archive old data** once migration is confirmed successful

---

## Benefits Summary

| Aspect | Old Structure | New Structure |
|--------|--------------|---------------|
| **Data Storage** | JSON blob | Normalized tables |
| **Querying** | Complex JSON parsing | Standard SQL queries |
| **Referential Integrity** | None | Full FK constraints |
| **Performance** | Slow for analytics | Optimized for queries |
| **Maintainability** | Difficult | Standard RDBMS patterns |
| **MiniBlock Association** | Ambiguous (global) | Clear (tied to block/line) |
| **Extensibility** | Limited | Easy to extend |

---

## Related Documentation

- [Language Categories](language-categories.md)
- [Puzzle Types](puzzle-types.md)
- [Fill-in-the-Blank Options](fill-in-blank-options.md)
