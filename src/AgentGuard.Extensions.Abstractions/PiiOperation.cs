namespace AgentGuard.Extensions.Abstractions;

/// <summary>
/// Defines the set of operations that can be performed on sensitive or secure data.
/// </summary>
/// <remarks>
/// This enumeration represents common data processing operations such as encryption, hashing, and masking.
/// It is typically used to specify the desired operation in data security workflows.
/// </remarks>
public enum PiiOperation
{
    /// <summary>None / Undefined / Unknown.</summary>
    Unknown = 0,

    /// <summary>Decrypt PII.</summary>
    Decrypt = 1,

    /// <summary>Encrypt PII.</summary>
    Encrypt = 2,

    /// <summary>Hash PII.</summary>
    Hash = 3,

    /// <summary>Mask PII with asterisks.</summary>
    Mask = 4,

    /// <summary>Redact PII.</summary>
    Redact = 5,

    /// <summary>Replace PII with a placeholder.</summary>
    Replace = 6,

    /// <summary>Remove PII.</summary>
    Remove = 10,

    /// <summary>Block the entire message.</summary>
    Block = 99
}