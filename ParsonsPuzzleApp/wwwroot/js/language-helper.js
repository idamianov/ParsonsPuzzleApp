/**
 * Language Helper - Provides dynamic language functionality for the frontend
 * Replaces hardcoded language arrays and magic numbers with API-driven approach
 */

class LanguageHelper {
    constructor() {
        this.languages = new Map();
        this.initialized = false;
    }

    /**
     * Initialize language data from API
     */
    async initialize() {
        if (this.initialized) return;

        try {
            const response = await fetch('/api/languageapi');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const languages = await response.json();
            
            // Store languages in map for quick lookup
            languages.forEach(lang => {
                this.languages.set(lang.id, lang);
            });
            
            this.initialized = true;
            console.log('Language helper initialized with', languages.length, 'languages');
        } catch (error) {
            console.error('Failed to initialize language helper:', error);
            // Fallback to default behavior if API fails
            this.initialized = false;
        }
    }

    /**
     * Get language by ID
     */
    getLanguage(id) {
        return this.languages.get(parseInt(id));
    }

    /**
     * Get all languages
     */
    getAllLanguages() {
        return Array.from(this.languages.values());
    }

    /**
     * Get CodeMirror mode for a language ID
     */
    getCodeMirrorMode(languageId) {
        if (!this.initialized) {
            // Fallback to original hardcoded logic
            return this.getFallbackCodeMirrorMode(languageId);
        }

        const language = this.getLanguage(languageId);
        return language ? language.codeMirrorMode : 'text/plain';
    }

    /**
     * Check if language is bracket-based
     */
    isBracketBased(languageId) {
        if (!this.initialized) {
            // Fallback logic
            return [1, 2, 3, 4, 5].includes(parseInt(languageId));
        }

        const language = this.getLanguage(languageId);
        return language ? language.isBracketBased : false;
    }

    /**
     * Get language display name
     */
    getDisplayName(languageId) {
        const language = this.getLanguage(languageId);
        return language ? language.displayName : 'Unknown';
    }

    /**
     * Get comment syntax for multiline blocks
     */
    getCommentSyntax(languageId) {
        const language = this.getLanguage(languageId);
        return language ? language.commentSyntax : '//';
    }

    /**
     * Generate multiline block help text
     */
    getMultilineHelp(languageId) {
        const language = this.getLanguage(languageId);
        if (!language) return '';

        const syntax = language.commentSyntax;
        return `
            <strong>Кога да използвате многоредови блокове:</strong><br>
            Използвайте многоредови блокове, когато имате последователни редове код, които са валидни независимо от реда им.<br><br>
            <strong>Синтаксис:</strong><br>
            <code>${syntax}--></code> за начало на многоредов блок<br>
            <code>${syntax}&lt;--</code> за край на многоредов блок<br><br>
            <strong>Пример:</strong><br>
            <code>${syntax}--><br>
            var x = 10;<br>
            var y = 20;<br>
            ${syntax}&lt;--</code>
        `;
    }

    /**
     * Fallback CodeMirror mode logic (original hardcoded approach)
     */
    getFallbackCodeMirrorMode(languageId) {
        const id = parseInt(languageId);
        switch (id) {
            case 1: case 2: case 3: case 4: return "clike";
            case 5: return "javascript";
            case 6: return "python";
            case 7: case 8: case 9: case 10: return "sql";
            default: return "text/plain";
        }
    }
}

// Create global instance
window.languageHelper = new LanguageHelper();

// Backward compatibility functions
window.getCodeMirrorMode = function() {
    const languageId = $("#languageSelect").val();
    return window.languageHelper.getCodeMirrorMode(languageId);
};

// Initialize on DOM ready
$(document).ready(async function() {
    await window.languageHelper.initialize();
});

