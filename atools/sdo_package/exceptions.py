"""
SDO Exception Classes
Custom exception hierarchy for structured error handling.
"""


class SDOError(Exception):
    """Base exception for all SDO errors."""
    
    def __init__(self, message: str, details: str = None):
        super().__init__(message)
        self.message = message
        self.details = details
    
    def __str__(self):
        return self.message


# Alias for backward compatibility and test expectations
SDOException = SDOError


class ConfigurationError(SDOError):
    """Raised when there are configuration issues."""
    pass


class ValidationError(SDOError):
    """Raised when validation fails."""
    pass


class AuthenticationError(SDOError):
    """Raised when authentication fails."""
    pass


class PlatformError(SDOError):
    """Raised when platform operations fail."""
    pass


class ParsingError(SDOError):
    """Raised when parsing fails."""
    pass


class APIError(SDOError):
    """Raised when API calls fail."""
    
    def __init__(self, message: str, status_code: int = None, response_text: str = None):
        super().__init__(message)
        self.status_code = status_code
        self.response_text = response_text
    
    def __str__(self):
        base_msg = self.message
        if self.status_code:
            base_msg += f" (HTTP {self.status_code})"
        return base_msg