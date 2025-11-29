"""
Tests for markdown parser functionality.
Tests parsing of work items, issues, and markdown content.
"""

import sys
from pathlib import Path

# Add the atools directory to sys.path
sys.path.insert(0, str(Path(__file__).parent.parent))

from sdo_package.parsers.markdown_parser import MarkdownParser  # noqa: E402


class TestMarkdownParserInit:
    """Test Markdown parser initialization."""

    def test_parser_initialization(self):
        """Test basic parser initialization."""
        parser = MarkdownParser()

        assert parser is not None
        assert hasattr(parser, "parse_workitem")
        assert hasattr(parser, "parse_issue")
        assert hasattr(parser, "validate_markdown")

    def test_parser_initialization_verbose(self):
        """Test parser initialization with verbose mode."""
        parser = MarkdownParser(verbose=True)

        assert parser.verbose is True


class TestMarkdownParserWorkitemParsing:
    """Test workitem markdown parsing."""

    def setup_method(self):
        """Set up test fixtures."""
        self.parser = MarkdownParser(verbose=True)

    def test_parse_workitem_basic(self):
        """Test parsing basic workitem markdown."""
        markdown_content = r"""# Product Backlog Item: Implement User Authentication

## Description
Implement secure user authentication system with OAuth2 support.

## Acceptance Criteria
- [ ] Users can log in with email/password
- [ ] OAuth2 integration with Google and GitHub
- [ ] Password reset functionality
- [ ] Session management

## Target: azdo
## Project: TestProject
## Area: TestProject\Development
## Iteration: TestProject\Sprint 1
## Labels: authentication, security, oauth2
"""

        result = self.parser.parse_workitem(markdown_content)

        assert result is not None
        assert result["title"] == "Implement User Authentication"
        assert "Implement secure user authentication" in result["description"]
        assert len(result["acceptance_criteria"]) == 4
        assert result["target"] == "azdo"
        assert result["project"] == "TestProject"
        assert result["area"] == r"TestProject\Development"
        assert result["iteration"] == r"TestProject\Sprint 1"
        assert "authentication" in result["labels"]

    def test_parse_workitem_minimal(self):
        """Test parsing minimal workitem markdown."""
        markdown_content = """# Task: Fix Bug

## Description
Simple bug fix.
"""

        result = self.parser.parse_workitem(markdown_content)

        assert result is not None
        assert result["title"] == "Fix Bug"
        assert result["description"] == "Simple bug fix."
        assert result["acceptance_criteria"] == []
        assert result["target"] is None
        assert result["project"] is None
        assert result["labels"] == []

    def test_parse_workitem_complex(self):
        """Test parsing complex workitem with all fields."""
        markdown_content = r"""# Epic: Complete System Redesign

## Description
This epic covers the complete redesign of our system architecture to improve scalability and maintainability.

## Acceptance Criteria
- [x] Database schema redesigned
- [ ] API endpoints updated
- [x] Frontend components migrated
- [ ] Documentation updated
- [ ] Tests passing

## Target: azdo
## Project: TestProject
## Area: TestProject\Architecture
## Iteration: TestProject\Sprint 2
## Labels: architecture, redesign, scalability, maintenance

## Additional Notes
This is a high-impact change that requires careful planning and testing.
"""

        result = self.parser.parse_workitem(markdown_content)

        assert result is not None
        assert result["title"] == "Complete System Redesign"
        assert "scalability and maintainability" in result["description"]
        assert len(result["acceptance_criteria"]) == 5
        assert result["acceptance_criteria"][0]["text"] == "Database schema redesigned"
        assert result["acceptance_criteria"][0]["completed"] is True
        assert result["acceptance_criteria"][1]["completed"] is False
        assert result["target"] == "azdo"
        assert result["project"] == "TestProject"
        assert len(result["labels"]) == 4
        assert "Additional Notes" in result["description"]

    def test_parse_workitem_empty(self):
        """Test parsing empty workitem markdown."""
        markdown_content = ""

        result = self.parser.parse_workitem(markdown_content)

        assert result is None

    def test_parse_workitem_no_title(self):
        """Test parsing workitem without title."""
        markdown_content = """## Description
Some description without title.
"""

        result = self.parser.parse_workitem(markdown_content)

        assert result is None

    def test_parse_workitem_malformed(self):
        """Test parsing malformed workitem markdown."""
        markdown_content = """# Title
Invalid markdown structure
"""

        result = self.parser.parse_workitem(markdown_content)

        assert result is not None
        assert result["title"] == "Title"
        assert result["description"] == "Invalid markdown structure"


class TestMarkdownParserIssueParsing:
    """Test issue markdown parsing."""

    def setup_method(self):
        """Set up test fixtures."""
        self.parser = MarkdownParser(verbose=True)

    def test_parse_issue_basic(self):
        """Test parsing basic issue markdown."""
        markdown_content = """# Bug: Login Button Not Working

## Target: github
## Repository: myorg/myrepo
## Labels: bug, frontend

## Description
The login button on the homepage doesn't respond to clicks.
"""

        result = self.parser.parse_issue(markdown_content)

        assert result is not None
        assert result["title"] == "Login Button Not Working"
        assert "doesn't respond to clicks" in result["description"]
        assert result["target"] == "github"
        assert result["repository"] == "myorg/myrepo"
        assert "bug" in result["labels"]
        assert "frontend" in result["labels"]

    def test_parse_issue_minimal(self):
        """Test parsing minimal issue markdown."""
        markdown_content = """# Feature Request: Dark Mode

## Target: github
## Repository: myorg/myrepo
## Labels: enhancement

## Description
Add dark mode support to the application.
"""

        result = self.parser.parse_issue(markdown_content)

        assert result is not None
        assert result["title"] == "Dark Mode"
        assert result["description"] == "Add dark mode support to the application."
        assert result["target"] == "github"
        assert result["repository"] == "myorg/myrepo"
        assert result["labels"] == ["enhancement"]

    def test_parse_issue_with_code_blocks(self):
        """Test parsing issue with code blocks."""
        markdown_content = """# Bug: API Call Failing

## Target: github
## Repository: myorg/api-repo
## Labels: bug, api

## Description
The API call to `/api/users` is failing with 500 error.

```bash
curl -X GET https://api.example.com/api/users \\
  -H "Authorization: Bearer token"
```

## Acceptance Criteria
- [ ] API returns 200 status
- [ ] User list is returned
"""

        result = self.parser.parse_issue(markdown_content)

        assert result is not None
        assert result["title"] == "API Call Failing"
        assert "500 error" in result["description"]
        assert "curl" in result["description"]
        assert result["target"] == "github"
        assert result["repository"] == "myorg/api-repo"
        assert result["labels"] == ["bug", "api"]

    def test_parse_issue_empty(self):
        """Test parsing empty issue markdown."""
        markdown_content = ""

        result = self.parser.parse_issue(markdown_content)

        assert result is None

    def test_parse_issue_no_title(self):
        """Test parsing issue without title."""
        markdown_content = """## Description
Some issue description.
"""

        result = self.parser.parse_issue(markdown_content)

        assert result is None


class TestMarkdownParserValidation:
    """Test markdown validation functionality."""

    def setup_method(self):
        """Set up test fixtures."""
        self.parser = MarkdownParser(verbose=True)

    def test_validate_markdown_valid_workitem(self):
        """Test validation of valid workitem markdown."""
        markdown_content = r"""# Task: Valid Workitem

## Target: azdo
## Project: TestProject
## Area: TestProject\Area
## Iteration: TestProject\Sprint 1
## Labels: enhancement, test

## Description
This is a valid workitem.

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2
"""

        result = self.parser.validate_markdown(markdown_content, "workitem")

        assert result["valid"] is True
        assert result["type"] == "workitem"
        assert len(result["errors"]) == 0
        assert len(result["warnings"]) == 0

    def test_validate_markdown_invalid_workitem(self):
        """Test validation of invalid workitem markdown."""
        markdown_content = """Some random text without proper structure."""

        result = self.parser.validate_markdown(markdown_content, "workitem")

        assert result["valid"] is False
        assert result["type"] == "workitem"
        assert len(result["errors"]) > 0

    def test_validate_markdown_valid_issue(self):
        """Test validation of valid issue markdown."""
        markdown_content = """# Bug: Valid Issue

## Target: github
## Repository: test/repo
## Labels: bug, high-priority

## Description
This is a valid issue.

## Steps to Reproduce
1. Step 1
2. Step 2

## Expected Behavior
Expected result.

## Actual Behavior
Actual result.
"""

        result = self.parser.validate_markdown(markdown_content, "issue")

        assert result["valid"] is True
        assert result["type"] == "issue"
        assert len(result["errors"]) == 0

    def test_validate_markdown_invalid_issue(self):
        """Test validation of invalid issue markdown."""
        markdown_content = """# Just a title"""

        result = self.parser.validate_markdown(markdown_content, "issue")

        assert result["valid"] is False
        assert result["type"] == "issue"
        assert len(result["errors"]) > 0

    def test_validate_markdown_unknown_type(self):
        """Test validation with unknown type."""
        markdown_content = """# Some Title

## Description
Some content.
"""

        result = self.parser.validate_markdown(markdown_content, "unknown")

        assert result["valid"] is False
        assert "Unsupported type" in str(result["errors"])

    def test_validate_markdown_empty_content(self):
        """Test validation of empty content."""
        result = self.parser.validate_markdown("", "workitem")

        assert result["valid"] is False
        assert len(result["errors"]) > 0


class TestMarkdownParserErrorHandling:
    """Test markdown parser error handling."""

    def setup_method(self):
        """Set up test fixtures."""
        self.parser = MarkdownParser()

    def test_parse_workitem_with_exception(self):
        """Test workitem parsing with exception."""
        # This would test internal parsing errors
        # For now, we'll test with None input
        result = self.parser.parse_workitem(None)

        assert result is None

    def test_parse_issue_with_exception(self):
        """Test issue parsing with exception."""
        result = self.parser.parse_issue(None)

        assert result is None

    def test_validate_markdown_with_exception(self):
        """Test validation with exception."""
        result = self.parser.validate_markdown(None, "workitem")

        assert result["valid"] is False
