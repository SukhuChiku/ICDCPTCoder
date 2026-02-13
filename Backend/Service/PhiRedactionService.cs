
using System.Text.RegularExpressions;

namespace Backend.Service;



/// <summary>
/// Comprehensive PHI redaction service using regex patterns and Named Entity Recognition (NER) rules.
/// Detects and redacts: structured data (IDs, dates), PII (names, addresses), clinical data (diagnoses, medications),
/// and contextual PHI in free text using domain-specific rules.
/// </summary>
public class PhiRedactionService : IPhiRedactionService
{
    // Structured Data Patterns
    private static readonly (string Pattern, string Placeholder)[] StructuredPhiPatterns =
    {
        // Identifiers
        (@"\bPatient\s+ID\s*:?\s*[A-Z0-9]{3,}\b", "[PATIENT_ID REDACTED]"),
        (@"\bVisit\s+ID\s*:?\s*\d{4,}\b", "[VISIT_ID REDACTED]"),
        (@"\bClaim\s+(?:Number|#)\s*:?\s*[A-Z0-9]{6,}\b", "[CLAIM_NUMBER REDACTED]"),
        (@"\bMember\s+ID\s*:?\s*[A-Z0-9]{6,}\b", "[MEMBER_ID REDACTED]"),
        (@"\bMedicaid\s+(?:Number|#)\s*:?\s*[A-Z0-9]{4,}\b", "[MEDICAID_ID REDACTED]"),
        (@"\bMedicare\s+(?:Number|#)\s*:?\s*[A-Z0-9]{4,}\b", "[MEDICARE_ID REDACTED]"),
        (@"\bRx\s*:?\s*\d{6,}\b|\bPrescription\s*:?\s*[A-Z0-9]{6,}\b", "[PRESCRIPTION_NUMBER REDACTED]"),
        (@"\bDL\s*:?\s*[A-Z0-9]{5,10}\b|\bDriver.{0,2}License\s*:?\s*[A-Z0-9]{5,10}\b", "[DRIVERS_LICENSE REDACTED]"),
        (@"\bPassport\s*:?\s*[A-Z0-9]{6,9}\b", "[PASSPORT REDACTED]"),
        
        // Social Security Number (XXX-XX-XXXX or XXXXXXXXX)
        (@"\bSSN\s*:?\s*\d{3}-\d{2}-\d{4}\b|\b\d{3}-\d{2}-\d{4}\b|\b\d{9}\b", "[SSN REDACTED]"),
        
        // Phone numbers (various formats)
        (@"\b(?:\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})\b", "[PHONE REDACTED]"),
        
        // Email addresses
        (@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", "[EMAIL REDACTED]"),
        
        // Credit card numbers (4-digit groups)
        (@"\b(?:\d{4}[-\s]?){3}\d{4}\b|\b\d{16}\b", "[CARD REDACTED]"),
        
        // Medical Record Number (MRN)
        (@"\bMRN\s*:?\s*\d{6,12}\b", "[MRN REDACTED]"),
        
        // Dates of birth (MM/DD/YYYY, MM-DD-YYYY, YYYY-MM-DD, Month DD, YYYY)
        (@"\b(?:(?:0[1-9]|1[0-2])[-/](?:0[1-9]|[12]\d|3[01])[-/](?:19|20)\d{2})\b", "[DOB REDACTED]"),
        (@"\b(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+(?:0?[1-9]|[12]\d|3[01]),?\s+(?:19|20)\d{2}\b", "[DOB REDACTED]"),
        (@"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(?:0?[1-9]|[12]\d|3[01]),?\s+(?:19|20)\d{2}\b", "[DOB REDACTED]"),
    };

    // Clinical Data Patterns
    private static readonly (string Pattern, string Placeholder)[] ClinicalPhiPatterns =
    {
        // Diagnoses (ICD-10 codes and common diagnoses)
        (@"\bICD-?10\s*:?\s*[A-Z]\d{2}(?:\.\d{1,4})?\b", "[DIAGNOSIS REDACTED]"),
        (@"\bDiagnosis\s*:?\s*[A-Za-z\s,&]+(?:,|\sand\s|$)", "[DIAGNOSIS REDACTED]"),
        (@"\b(?:Type\s+2\s+)?Diabetes\b|\bHypertension\b|\bAsthma\b|\bCOPD\b|\bCancer\b|\bHIV\b|\bAIDS\b", "[CONDITION REDACTED]"),
        
        // Symptoms
        (@"\bSymptoms?\s*:?\s*[A-Za-z\s,&]+(?:,|\.|\sand\s|$)", "[SYMPTOMS REDACTED]"),
        
        // Medications
        (@"\bMedications?\s*:?\s*[A-Za-z0-9\s,&-]+(?:,|\.|\sand\s|$)", "[MEDICATION REDACTED]"),
        (@"\b(?:Aspirin|Metformin|Lisinopril|Metoprolol|Atorvastatin|Levothyroxine|Amlodipine|Amoxicillin|Ibuprofen|Omeprazole)\b", "[MEDICATION REDACTED]"),
        
        // Lab results and values
        (@"\bLab\s+(?:Results?|Values?)\s*:?\s*[A-Za-z0-9\s,.\(\)]+", "[LAB_RESULTS REDACTED]"),
        (@"\b(?:Hemoglobin|HbA1c|Glucose|Triglycerides|Cholesterol|Creatinine|BUN|ALT|AST)\s*:?\s*[\d.]+\s*(?:mg/dL|mmol/L|%|g/dL)?\b", "[LAB_VALUE REDACTED]"),
        
        // Mental health and sensitive conditions
        (@"\b(?:Depression|Anxiety|Bipolar|Schizophrenia|Psychosis|PTSD|Trauma)\b", "[MENTAL_HEALTH REDACTED]"),
        
        // Substance use
        (@"\b(?:Alcohol\s+(?:use|abuse|dependence)|Drug\s+(?:use|abuse|dependence)|Smoking|Tobacco|Cocaine|Heroin|Methamphetamine)\b", "[SUBSTANCE_USE REDACTED]"),
        
        // Genetic and reproductive info
        (@"\b(?:Genetic\s+(?:test|marker|mutation)|BRCA|Cystic\s+fibrosis|Sickle\s+cell)\b", "[GENETIC_INFO REDACTED]"),
        (@"\b(?:Pregnant|Pregnancy|Miscarriage|Abortion|Fertility|Contraceptive)\b", "[REPRODUCTIVE REDACTED]"),
        
        // HIV/AIDS status
        (@"\b(?:HIV(?:\s+positive|\s+negative|/AIDS)|AIDS)\b", "[HIV_STATUS REDACTED]"),
        
        // Rare/specific diseases
        (@"\b(?:Lupus|Lyme\s+disease|Crohn|Celiac|Hashimoto)\b", "[RARE_DISEASE REDACTED]"),
    };

    // NER Domain Rules - Contextual PHI detection
    private static readonly (string Pattern, string Placeholder)[] DomainContextPatterns =
    {
        // Gender terms
        (@"\b(?:male|female|man|woman|boy|girl)\b", "[GENDER REDACTED]"),
        
        // Family member names: "wife Linda", "husband John", etc.
        (@"(?:wife|husband|daughter|son|mother|father|sister|brother|grandmother|grandfather|aunt|uncle|niece|nephew|cousin|partner|spouse)\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)", "[FAMILY_MEMBER REDACTED]"),
        
        // Company/Employer: "works at Google", "employed by Apple"
        (@"(?:works\s+at|employed\s+by|works\s+for|employee\s+of)\s+([A-Z][A-Za-z&\s.]+?)(?:\.|,|;|$)", "[EMPLOYER REDACTED]"),
        
        // Locations: "lives in Sunnyvale", "moved to Boston"
        (@"(?:lives\s+in|moved\s+to|lives\s+on|address|located\s+at|from|hometown|city|town)\s+([A-Z][a-z]+(?:\s+(?:CA|TX|NY|FL|PA|IL|OH|GA|NC|MI|NJ|VA|WA|AZ|MA|TN|IN|MO|MD|WI|CO|MN|SC|AL|LA|KY|OR|OK|CT|UT|NV|AR|MS|KS|NM|NE|ID|HI|NH|ME|MT|RI|DE|SD|ND|AK|VT|WY)?)?(?:\.|,|;|$))", "[LOCATION REDACTED]"),
        
        // Complete street addresses with optional city/state: "123 Main St, San Jose, CA" or "4683 henrick ave,san jose,ca"
        (@"\b\d+\s+[A-Za-z\s]+(?:Street|street|St|st|Avenue|avenue|Ave|ave|Road|road|Rd|rd|Boulevard|boulevard|Blvd|blvd|Lane|lane|Ln|ln|Drive|drive|Dr|dr|Court|court|Ct|ct|Circle|circle|Cir|cir|Plaza|plaza|Way|way|Trail|trail|Terrace|terrace|Ter|ter|Place|place|Pl|pl)(?:\s*,?\s*[A-Za-z\s]+(?:\s+(?:CA|TX|NY|FL|PA|IL|OH|GA|NC|MI|NJ|VA|WA|AZ|MA|TN|IN|MO|MD|WI|CO|MN|SC|AL|LA|KY|OR|OK|CT|UT|NV|AR|MS|KS|NM|NE|ID|HI|NH|ME|MT|RI|DE|SD|ND|AK|VT|WY))?)?", "[ADDRESS REDACTED]"),
        
        // Zip codes (5 or 9 digit)
        (@"\b\d{5}(?:-\d{4})?\b", "[ZIPCODE REDACTED]"),
        
        // Age patterns: "45 year old", "28 years old"
        (@"\b(?:is\s+)?(\d{1,2})\s+(?:year|yo|y/o)s?\s+old\b", "[AGE REDACTED]"),
        
        // Initials of multiple people: "J.D.", "M.P.", etc
        (@"\b[A-Z](?:\.|(?=\s+[A-Z]))\s+[A-Z]\.\b", "[INITIALS REDACTED]"),
    };

    public (string RedactedText, bool WasRedacted) RedactPhiData(string text)
    {
        if (string.IsNullOrEmpty(text))
            return (text, false);

        string redactedText = text;
        bool wasRedacted = false;

        // Apply structured PHI patterns
        foreach (var (pattern, placeholder) in StructuredPhiPatterns)
        {
            if (ApplyPatternRedaction(ref redactedText, pattern, placeholder))
                wasRedacted = true;
        }

        // Apply clinical PHI patterns
        foreach (var (pattern, placeholder) in ClinicalPhiPatterns)
        {
            if (ApplyPatternRedaction(ref redactedText, pattern, placeholder))
                wasRedacted = true;
        }

        // Apply domain context patterns for NER-style detection
        foreach (var (pattern, placeholder) in DomainContextPatterns)
        {
            if (ApplyPatternRedaction(ref redactedText, pattern, placeholder))
                wasRedacted = true;
        }

        // Additional free-text heuristics for name detection
        if (DetectAndRedactNames(ref redactedText))
            wasRedacted = true;

        return (redactedText, wasRedacted);
    }

    private bool ApplyPatternRedaction(ref string text, string pattern, string placeholder)
    {
        string original = text;
        text = Regex.Replace(text, pattern, placeholder, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        return text != original;
    }

    /// <summary>
    /// Heuristic detection of proper names in text.
    /// Catches patterns like:
    /// - "Jonathan Miller" (capitalized word + capitalized word in patient context)
    /// - "patient named X Y", "doctor X Y"
    /// - Names appearing after clinical verbs
    /// </summary>
    private bool DetectAndRedactNames(ref string text)
    {
        bool changed = false;
        string original = text;

        // Pattern 1: Context-based names "patient named X Y", "doctor X", etc.
        var contextPattern = @"(?:patient\s+(?:named|is)|doctor|nurse|physician|provider|called|known\s+as|contact|by|physician|staff|seen\s+by)\s+(?:[A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)";
        text = Regex.Replace(text, contextPattern, m =>
        {
            var prefix = Regex.Match(m.Value, @"^[A-Za-z\s]+\s+").Value;
            return prefix + "[NAME REDACTED]";
        }, RegexOptions.IgnoreCase);

        if (text != original)
            changed = true;

        original = text;

        // Pattern 2: Proper name format "FirstName LastName" at sentence start or after punctuation/keywords
        // Catches: "Jonathan Miller presents", "John Smith was seen"
        var properNamePattern = @"(?:^|\s|:\s|,\s|presents|was\s+seen|comes\s+in|reports)([A-Z][a-z]{2,}\s+[A-Z][a-z]{2,})(?:\s+(?:presents|was|comes|reports|states|denies|complains)|\.|,|;|$)";
        text = Regex.Replace(text, properNamePattern, m =>
        {
            var prefix = Regex.Match(m.Value, @"^[^\w]*").Value;  // Get leading whitespace/punctuation
            return prefix + "[NAME REDACTED]";
        }, RegexOptions.Multiline);

        if (text != original)
            changed = true;

        return changed;
    }
}