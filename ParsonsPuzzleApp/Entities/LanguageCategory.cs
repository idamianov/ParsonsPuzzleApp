namespace ParsonsPuzzleApp.Entities
{
    public enum LanguageCategory
    {
        /// <summary>
        /// Languages that use curly braces {} for code blocks (C, C++, C#, Java, JavaScript)
        /// </summary>
        Bracket = 1,
        
        /// <summary>
        /// Languages where indentation is syntactically significant (Python)
        /// </summary>
        Indentation = 2,
        
        /// <summary>
        /// SQL-based languages (T-SQL, MySQL, PostgreSQL, PL/SQL)
        /// </summary>
        SQL = 3
    }
}
