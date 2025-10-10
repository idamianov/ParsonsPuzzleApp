# Parsons-Puzzles-Toolkit

The **Parsons Puzzle Toolkit**, recently presented at [FMNS-2025 in Blagoevgrad, Bulgaria](https://fmns2025.swu.bg/), represents a significant advancement in educational tools aimed at strengthening computational thinking and code comprehension skills. Building upon the original concept introduced by [Dale Parsons and Patricia Haden in 2006](https://dl.acm.org/doi/10.5555/1151869.1151890), this new iteration addresses the limitations of earlier implementations by introducing a comprehensive and highly adaptable framework designed to support a wider range of programming languages, educational contexts, and cognitive learning strategies.

<img width="949" height="557" alt="image" src="https://github.com/user-attachments/assets/9d2c0dbe-94d7-4d74-8789-2d33c99a3912" />

Parsons Puzzles are structured as code-reordering exercises, where students reconstruct correct programs from shuffled code lines. By removing the burden of syntax, these puzzles allow learners to focus on logical sequencing and algorithmic reasoning. However, traditional Parsons Puzzle tools tend to be confined to one or two languages - often with similar imperative structures - and offer limited flexibility in task design. The **Parsons Puzzle Toolkit** overcomes these constraints by introducing multi-language support (C, C++, C#, Java, JavaScript, Python, and SQL), multi-paradigm compatibility, and advanced features that support more complex learning scenarios.

Key innovations include:

* **Support for multi-line and nested code blocks**, allowing instructors to design more realistic and complex programming exercises that reflect the structure of actual codebases.
* **Mini-blocks**, which segment tasks into smaller, concept-focused components, making it easier for learners to engage with specific programming constructs or logical subroutines.
* **Customizable task design**, enabling educators to create puzzles tailored to different skill levels, languages, or conceptual goals.

<img width="925" height="436" alt="image" src="https://github.com/user-attachments/assets/40a200cc-e19a-4d69-a7ee-e96f20b2c12f" />

Beyond its technical features, the toolkit is also pedagogically significant. By blending the visual intuitiveness of block-based learning with the rigor of text-based coding, the toolkit functions as a bridge for students transitioning from environments like Scratch to modern general-purpose languages. This hybrid model supports learners at various levels, from absolute beginners to those making the leap into industry-standard development languages.

The toolkit's visual interface and logical segmentation support through structured, engaging, and interactive problem-solving tasks the core elements of computational thinking:
- **decomposition**,
- **pattern recognition**,
- **abstraction**,
- **algorithmic design**.

This positions the Parsons Puzzle Toolkit not only as an effective classroom tool but also as a foundational component in the broader landscape of computer science education.

Moreover, the toolkit is currently undergoing **active development**, with new features in progress, including **automated evaluation and feedback mechanisms**. These forthcoming additions aim to provide real-time guidance and adaptive learning pathways, thereby enhancing the toolkit's effectiveness as both a formative and summative assessment tool.

> [!Note]
> A full research article detailing the theoretical foundations, technical implementation, and educational applications of the Parsons Puzzle Toolkit is currently under review for publication in a peer-reviewed computer science education journal.

By expanding the pedagogical reach and technical capacity of traditional Parsons Puzzles, this toolkit offers a future-ready solution for programming education. Whether in introductory computer science courses, specialized programming tracks, or database instruction, it delivers a highly adaptable and impactful learning experience that aligns with modern educational goals.

---

## Technologies Used

* ASP.NET Core Razor Pages
* Entity Framework Core
* SQLite

---

## Getting Started

In order to get your SQLite DB created execute the following

```bash
dotnet ef database update
```

---

## Sample Parsons Puzzles

Try out these live examples to see the toolkit in action:

- [C++ Standard 2D Puzzle](http://194.141.86.248/bundle/b07884e9-22e0-4004-81a8-05aaa04d83bc) (Key to Unlock: `123`)
- [Python Standard 2D Puzzle](http://194.141.86.248/bundle/75489b30-b603-4560-9606-a094f9347642) (Key to Unlock: `123`)
- [Fill-in-the-blank SQL Puzzle](http://194.141.86.248/bundle/6abf8830-205f-4009-ad01-210012459896) (Key to Unlock: `123`)

---

## Documentation

Comprehensive technical documentation is available in the [`Docs/`](Docs/) folder. This documentation provides in-depth information about the system's architecture, design decisions, and implementation details.

### Architecture & Design

#### [Database Model](Docs/database-model.md)
Learn about the evolution of the database structure for storing puzzles and student solutions:
- **Old vs New normalized structure** – Analysis of the transition from JSON-based storage to a fully normalized relational model
- **Entity relationships** – Detailed breakdown of Puzzle, PuzzleBlock, PuzzleBlockLine, and MiniBlock entities
- **Solution storage strategies** – How student attempts are stored and validated
- **Migration approach** – Guidance on migrating from the old structure to the new normalized approach
- **Benefits summary** – Performance improvements, querying capabilities, and maintainability gains

#### [Language Categories](Docs/language-categories.md)
Understand how different programming languages are handled in the system:
- **Bracket-based languages** (C#, Java, JavaScript, C++, C) – How curly-brace languages are parsed and validated
- **Indentation-based languages** (Python, YAML, Ruby, Bash) – Handling whitespace-sensitive syntax
- **SQL-based languages** – Special considerations for database query languages
- **Parsing differences** – Block detection, statement separation, and nesting strategies
- **Validation strategies** – Language-specific validation logic
- **Frontend considerations** – How to display and interact with different language types

### Puzzle Types

#### [Puzzle Types Overview](Docs/puzzle-types.md)
Explore the different types of puzzles supported by the toolkit:
- **Rearrangement Puzzles** – Traditional drag-and-drop code block ordering
  - Data structures and validation logic
  - Frontend implementation (drag-and-drop with jQuery UI)
  - Visual feedback and error handling
- **Fill-in-the-Blank Puzzles** – Complete missing code elements using MiniBlocks
  - Input field types and validation
  - Slot-based answer management
  - Real-time feedback mechanisms
- **Hybrid Puzzles** – Combining both rearrangement and fill-in-the-blank
  - Implementation considerations
  - Combined validation logic
- **Complete code examples** – HTML, JavaScript, CSS, and C# implementation details

#### [Fill-in-the-Blank Options](Docs/fill-in-blank-options.md)
Detailed guide on implementing fill-in-the-blank puzzles with various input methods:
- **Input Method Options:**
  - Free text input (maximum flexibility, tests recall)
  - Dropdown selection (prevents typos, tests recognition)
  - Code editor (Monaco/CodeMirror for complex expressions)
  - Drag-and-drop tokens (visual, error-free interaction)
  - Auto-complete input (combines flexibility with guidance)
- **Validation Strategies:**
  - Exact match, case-insensitive matching
  - Multiple acceptable answers
  - Pattern matching with regular expressions
  - Semantic validation (compile/execute code)
  - Partial credit scoring
- **User Experience Considerations:**
  - Inline vs separate input fields
  - Real-time vs submit-based validation
  - Progressive hint systems
  - Visual feedback mechanisms (color coding, icons, animations)
- **Advanced Features:**
  - Context-aware suggestions based on surrounding code
  - Intelligent error messages tailored to common mistakes
  - Adaptive difficulty based on student performance
- **Recommendations by skill level** – Best practices for beginners, intermediate, and advanced students

### Quick Start Guide

For developers new to the codebase:
1. Start with [Database Model](Docs/database-model.md) to understand data structures
2. Review [Language Categories](Docs/language-categories.md) to understand language handling
3. Explore [Puzzle Types](Docs/puzzle-types.md) for implementation patterns
4. Deep dive into [Fill-in-the-Blank Options](Docs/fill-in-blank-options.md) for advanced features

---

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for significant changes
- Ensure database migrations are included
- Test across different browsers

---

## Acknowledgments

- Parsons Puzzles concept by [Dale Parsons and Patricia Haden](https://dl.acm.org/doi/10.5555/1151869.1151890)
- Educational research on code puzzles in CS education
- Open-source libraries and frameworks used in this project
- Presented at [FMNS-2025 in Blagoevgrad, Bulgaria](https://fmns2025.swu.bg/)

---

## Roadmap

### Current Version
- ✅ Multi-language support (C, C++, C#, Java, JavaScript, Python, SQL)
- ✅ Rearrangement and fill-in-the-blank puzzles
- ✅ Multi-line and nested code blocks
- ✅ Mini-blocks for concept-focused learning
- ✅ Normalized database structure
- ✅ Student progress tracking

### Planned Features
- 🔄 **Automated evaluation and feedback mechanisms** (in active development)
- 🔄 Advanced analytics and reporting
- 🔄 Adaptive learning pathways
- 🔄 Mobile-responsive design improvements
- 🔄 API for third-party integrations
- 🔄 Gamification elements (badges, leaderboards)
- 🔄 Export/import puzzle collections
- 🔄 Code execution and testing integration

---

## License

[Specify your license here]

---

## Contact

[Your contact information or team details]
