import DOMPurify, { Config } from 'dompurify';

/**
 * HTML Sanitizer Service
 * Provides secure HTML sanitization using DOMPurify with predefined configurations
 */

// Configuration for rich text editor content (more permissive)
const RICH_TEXT_CONFIG = {
  ALLOWED_TAGS: [
    'p',
    'br',
    'div',
    'span',
    'strong',
    'b',
    'em',
    'i',
    'u',
    's',
    'strike',
    'ul',
    'ol',
    'li',
    'h1',
    'h2',
    'h3',
    'h4',
    'h5',
    'h6',
    'blockquote',
    'pre',
    'code',
    'a',
    'img',
    'table',
    'thead',
    'tbody',
    'tr',
    'td',
    'th',
    'sub',
    'sup',
  ],
  ALLOWED_ATTR: [
    'class',
    'style',
    'id',
    'href',
    'target',
    'rel',
    'src',
    'alt',
    'width',
    'height',
    'colspan',
    'rowspan',
    'align',
    'valign',
  ],
  ALLOWED_URI_REGEXP:
    /^(?:(?:(?:f|ht)tps?|mailto|tel|callto|cid|xmpp|#):|[^a-z]|[a-z+.-]+(?:[^a-z+.-:]|$))/i,
  KEEP_CONTENT: true,
};

// Configuration for basic text with simple formatting (more restrictive)
const BASIC_TEXT_CONFIG = {
  ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'u', 'ul', 'ol', 'li'],
  ALLOWED_ATTR: ['class'],
  KEEP_CONTENT: true,
};

// Configuration for plain text only (most restrictive)
const PLAIN_TEXT_CONFIG = {
  ALLOWED_TAGS: ['br'],
  ALLOWED_ATTR: [],
  KEEP_CONTENT: true,
};

export class SanitizerService {
  /**
   * Sanitizes rich text content from WYSIWYG editors
   * Allows most HTML formatting while preventing XSS attacks
   */
  static sanitizeRichText(content: string): string {
    if (!content) return '';
    return DOMPurify.sanitize(content, RICH_TEXT_CONFIG);
  }

  /**
   * Sanitizes basic text with simple formatting
   * Allows basic formatting like bold, italic, lists
   */
  static sanitizeBasicText(content: string): string {
    if (!content) return '';
    return DOMPurify.sanitize(content, BASIC_TEXT_CONFIG);
  }

  /**
   * Sanitizes content to plain text with line breaks only
   * Most restrictive - only allows line breaks
   */
  static sanitizePlainText(content: string): string {
    if (!content) return '';
    // Convert line breaks to HTML breaks first
    const contentWithBreaks = content.replace(/\n/g, '<br>');
    return DOMPurify.sanitize(contentWithBreaks, PLAIN_TEXT_CONFIG);
  }

  /**
   * Sanitizes content with custom configuration
   * For specific use cases requiring custom rules
   */
  static sanitizeCustom(content: string, config: Config): string {
    if (!content) return '';
    return DOMPurify.sanitize(content, config);
  }

  /**
   * Strips all HTML tags and returns plain text
   * Useful for search indexing or text-only contexts
   */
  static stripHtml(content: string): string {
    if (!content) return '';
    return DOMPurify.sanitize(content, {
      ALLOWED_TAGS: [],
      ALLOWED_ATTR: [],
      KEEP_CONTENT: true,
    });
  }

  /**
   * Validates if content is safe (doesn't contain malicious code)
   * Returns true if content is safe, false otherwise
   */
  static isSafe(content: string): boolean {
    if (!content) return true;
    const sanitized = DOMPurify.sanitize(content, RICH_TEXT_CONFIG);
    return sanitized === content;
  }
}

// Export default configurations for advanced usage
export const SANITIZER_CONFIGS = {
  RICH_TEXT: RICH_TEXT_CONFIG,
  BASIC_TEXT: BASIC_TEXT_CONFIG,
  PLAIN_TEXT: PLAIN_TEXT_CONFIG,
} as const;

export default SanitizerService;
