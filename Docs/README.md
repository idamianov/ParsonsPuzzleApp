# Parsons Puzzle Application - Technical Documentation

Welcome to the technical documentation for the Parsons Puzzle Application. This documentation provides in-depth information about the system's architecture, design decisions, and implementation details.

---

## Table of Contents

### 1. [Database Model](database-model.md)
**Analysis of the database model for solutions storage**

Learn about the evolution from the old JSON-based structure to the new normalized approach:
- Old structure limitations
- New normalized entity relationships
- Puzzle, PuzzleBlock, PuzzleBlockLine, and MiniBlock entities
- Solution storage strategies
- Migration approach
- Benefits of normalized data

**Key Topics:**
- Entity relationship design
- Data normalization
- Solution storage evolution
- Performance improvements

---

### 2. [Language Categories](language-categories.md)
**Analysis of bracket-based vs non-bracket languages interpretation**

Understand how the system handles different programming language categories:
- Bracket-based languages (C#, Java, JavaScript, C++, C)
- Indentation-based languages (Python, YAML, Ruby, Bash)
- SQL-based languages
- Language entity structure after refactoring
- Parsing differences
- Validation logic
- Frontend considerations

**Key Topics:**
- Language classification system
- Parsing strategies per language type
- Block detection algorithms
- Student experience considerations
- Best practices for each category

---

### 3. [Puzzle Types](puzzle-types.md)
**Analysis of puzzle types - rearrangement vs fill-in-the-blank**

Explore the two main puzzle types and their implementations:
- **Rearrangement Puzzles**: Drag-and-drop code blocks into correct order
- **Fill-in-the-Blank Puzzles**: Complete missing code elements
- **Hybrid Puzzles**: Combining both approaches
- Data structure differences
- Frontend implementation (HTML, JavaScript, CSS)
- Backend validation logic
- User interaction patterns

**Key Topics:**
- Puzzle type comparison
- Learning objectives for each type
- Technical implementation details
- Drag-and-drop functionality
- Input field management
- Validation strategies
- Combining puzzle types

---

### 4. [Fill-in-the-Blank Options](fill-in-blank-options.md)
**Options for implementing fill-in-the-blank puzzles**

Comprehensive guide to different approaches for fill-in-the-blank puzzles:

**Input Methods:**
- Free text input
- Dropdown selection
- Code editor (Monaco/CodeMirror)
- Drag-and-drop tokens
- Auto-complete input

**Validation Strategies:**
- Exact match
- Case-insensitive matching
- Multiple acceptable answers
- Pattern matching (Regex)
- Semantic validation
- Partial credit

**User Experience:**
- Inline vs separate input
- Real-time vs submit-based validation
- Progressive hint system
- Visual feedback mechanisms
- Advanced features

**Key Topics:**
- Input method trade-offs
- Validation complexity
- Context-aware suggestions
- Intelligent error messages
- Adaptive difficulty
- Recommendations by skill level

---

## Quick Reference

### For Developers

**Setting up a new language:**
1. Read [Language Categories](language-categories.md) to understand language classification
2. Determine the appropriate `LanguageCategory` for your language
3. Configure parsing rules based on the category
4. Test with sample puzzles

**Creating a new puzzle type:**
1. Review [Puzzle Types](puzzle-types.md) for implementation patterns
2. Review [Fill-in-the-Blank Options](fill-in-blank-options.md) if using MiniBlocks
3. Understand the [Database Model](database-model.md) for data storage
4. Implement frontend and backend validation

**Extending the database:**
1. Study the [Database Model](database-model.md) to understand current structure
2. Plan your changes to maintain normalization
3. Create Entity Framework migrations
4. Update validation logic as needed

---

## Documentation Conventions

### Code Examples
- C# code examples use PascalCase and follow .NET conventions
- JavaScript code examples use camelCase
- SQL uses UPPERCASE for keywords

### Diagrams
- Entity relationship diagrams show database structure
- Sequence diagrams show interaction flows
- Code structure examples use standard formatting

### Best Practices
Each document includes a "Best Practices" section with:
- Recommended approaches
- Common pitfalls to avoid
- Performance considerations
- Security considerations

---

## Related Resources

### External Documentation
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)
- [jQuery UI Documentation](https://jqueryui.com/)

### Academic Research
- Parsons, D., & Haden, P. (2006). "Parsons programming puzzles: a fun and effective learning tool for first programming courses"
- Research on code puzzles in computer science education

### Tools & Libraries
- Monaco Editor (code editor component)
- jQuery and jQuery UI (drag-and-drop)
- Bootstrap (UI framework)

---

## Contributing to Documentation

When updating documentation:
1. Maintain consistency with existing format
2. Include code examples for technical concepts
3. Add cross-references to related documents
4. Update the Table of Contents if adding new sections
5. Proofread for clarity and accuracy

---

## Getting Help

If you need clarification on any topic:
1. Check the relevant documentation file
2. Review code examples in the repository
3. Open an issue with specific questions
4. Contact the development team

---

## Document History

| Document | Created | Last Updated | Status |
|----------|---------|--------------|--------|
| database-model.md | 2025-10-10 | 2025-10-10 | Current |
| language-categories.md | 2025-10-10 | 2025-10-10 | Current |
| puzzle-types.md | 2025-10-10 | 2025-10-10 | Current |
| fill-in-blank-options.md | 2025-10-10 | 2025-10-10 | Current |

---

**Last Updated:** October 10, 2025
**Maintained by:** Development Team
