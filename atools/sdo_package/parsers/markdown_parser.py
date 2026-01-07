"""
Markdown Parser for SDO work item files.
"""

import re
from typing import Dict, List, Any, Optional


class MarkdownParser:
    """Parser for markdown files containing work item information."""

    def __init__(self, verbose: bool = False):
        """Initialize the MarkdownParser.

        Args:
            verbose: Enable verbose output for debugging
        """
        self.verbose = verbose

    def parse_file(self, file_path: str) -> Dict[str, Any]:
        """
        Parse a markdown file and extract work item information.

        Args:
            file_path: Path to the markdown file

        Returns:
            Dictionary containing:
            - title: Work item title
            - description: Main description
            - metadata: Dictionary of metadata fields
            - acceptance_criteria: List of acceptance criteria
        """
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()

        return self.parse_content(content)

    def parse_content(self, content: str) -> Dict[str, Any]:
        """
        Parse markdown content and extract work item information.

        Args:
            content: Raw markdown content

        Returns:
            Dictionary with parsed information
        """
        lines = content.split("\n")

        # Initialize result
        result = {
            "title": "",
            "description": "",
            "metadata": {},
            "acceptance_criteria": [],
            "repro_steps": "",
        }

        # State tracking
        in_acceptance_criteria = False
        in_repro_steps = False
        description_complete = False
        description_lines = []
        repro_lines = []

        for line in lines:
            line = line.strip()

            # Skip empty lines
            if not line:
                if description_lines and not in_acceptance_criteria:
                    description_lines.append("")
                continue

            # Extract title (first # heading)
            if line.startswith("# ") and not result["title"]:
                title = line[2:].strip()
                # Strip common work item type prefixes
                prefixes_to_strip = [
                    "product backlog item:",
                    "pbi:",
                    "task:",
                    "bug:",
                    "issue:",
                    "epic:",
                    "feature request:",
                    "user story:",
                    "feature:",
                    "enhancement:",
                    "bug report:",
                    "question:",
                    "discussion:",
                ]
                for prefix in prefixes_to_strip:
                    if title.lower().startswith(prefix):
                        title = title[len(prefix) :].strip()
                        break
                result["title"] = title
                continue

            # Extract metadata (## Key: Value format)
            metadata_match = re.match(r"^##\s*([^:]+):\s*(.*)$", line)
            if metadata_match:
                key = metadata_match.group(1).strip().lower().replace(" ", "_")
                value = metadata_match.group(2).strip()
                result["metadata"][key] = value
                continue

            # Check for Steps to Reproduce
            if "steps to reproduce" in line.lower():
                in_repro_steps = True
                continue

            # Check for Acceptance Criteria section
            if line.lower() in [
                "## acceptance criteria",
                "## acceptance criteria:",
                "### acceptance criteria",
                "### acceptance criteria:",
            ]:
                in_acceptance_criteria = True
                continue

            # Check for sections that should stop description collection
            if line.lower() in [
                "## implementation details",
                "## dependencies",
                "## testing requirements",
                "## definition of done",
                "## effort estimate",
                "## implementation details:",
                "## dependencies:",
                "## testing requirements:",
                "## definition of done:",
                "## effort estimate:",
            ]:
                # Stop collecting description when we hit implementation details or other sections
                description_complete = True
                in_acceptance_criteria = False
                continue

            # Extract repro steps
            if in_repro_steps:
                if line.startswith("**") or line.startswith("##"):
                    in_repro_steps = False
                else:
                    repro_lines.append(line)
                    continue

            # Extract acceptance criteria
            if in_acceptance_criteria:
                # Check for new section starting
                if line.startswith("#"):
                    in_acceptance_criteria = False
                    description_lines.append(line)
                    continue

                # Extract criteria items (both checked and unchecked)
                criteria_match = re.match(r"^[-*]\s*\[\s*([xX\s]?)\s*\]\s*(.+)$", line)
                if criteria_match:
                    completed = criteria_match.group(1).strip().lower() in ["x"]
                    text = criteria_match.group(2).strip()
                    result["acceptance_criteria"].append({"text": text, "completed": completed})
                    continue

                # Handle numbered criteria
                numbered_match = re.match(r"^\d+\.\s*(.+)$", line)
                if numbered_match:
                    result["acceptance_criteria"].append(
                        {"text": numbered_match.group(1).strip(), "completed": False}
                    )
                    continue

            # Add to description if not in special sections
            if not in_acceptance_criteria and not description_complete and not line.startswith("#") and not line.startswith("**Parent PBI:**"):
                description_lines.append(line)

        # Join description lines
        result["description"] = "\n".join(description_lines).strip()

        # Join repro steps
        result["repro_steps"] = "\n".join(repro_lines).strip()

        return result

    def validate_structure(self, parsed_content: Dict[str, Any]) -> List[str]:
        """
        Validate the parsed content structure.

        Args:
            parsed_content: Result from parse_content()

        Returns:
            List of validation error messages (empty if valid)
        """
        errors = []

        if not parsed_content.get("title"):
            errors.append("Missing title (no # heading found)")

        if not parsed_content.get("metadata"):
            errors.append("No metadata found (no ## Key: Value pairs)")

        return errors

    def parse_workitem(self, content: str) -> Optional[Dict[str, Any]]:
        """Parse workitem-specific markdown content.

        Args:
            content: Markdown content for a work item

        Returns:
            Parsed work item data or None on error/invalid content
        """
        try:
            parsed = self.parse_content(content)

            # Return None if no title
            if not parsed.get("title"):
                return None

            # Flatten metadata into top level for easier access
            result = {
                "title": parsed["title"],
                "description": parsed["description"],
                "acceptance_criteria": parsed["acceptance_criteria"],
            }

            # Add metadata fields, setting missing ones to appropriate defaults
            common_metadata_fields = [
                "target",
                "project",
                "area",
                "iteration",
                "assignee",
                "labels",
                "work_item_type",
                "repository",
            ]
            for field in common_metadata_fields:
                value = parsed["metadata"].get(field, None)
                if field == "labels" and value is not None:
                    # Split comma-separated labels into list
                    if isinstance(value, str):
                        value = [label.strip() for label in value.split(",") if label.strip()]
                    elif not isinstance(value, list):
                        value = [value]
                elif field == "labels" and value is None:
                    value = []  # Default labels to empty list
                result[field] = value

            # Add any additional metadata fields
            for key, value in parsed["metadata"].items():
                normalized_key = key.replace("_", " ")
                if normalized_key not in common_metadata_fields:
                    result[key] = value

            return result
        except Exception as e:
            if self.verbose:
                print(f"Error parsing work item: {e}")
            return None

    def parse_issue(self, content: str) -> Optional[Dict[str, Any]]:
        """Parse issue-specific markdown content.

        Args:
            content: Markdown content for an issue

        Returns:
            Parsed issue data or None on error/invalid content
        """
        try:
            parsed = self.parse_content(content)

            # Return None if no title
            if not parsed.get("title"):
                return None

            # Flatten metadata into top level for easier access
            result = {
                "title": parsed["title"],
                "description": parsed["description"],
                "acceptance_criteria": parsed["acceptance_criteria"],
            }

            # Add metadata fields, setting missing ones to appropriate defaults
            common_metadata_fields = ["target", "repository", "assignee", "labels"]
            for field in common_metadata_fields:
                value = parsed["metadata"].get(field, None)
                if field == "labels" and value is not None:
                    # Split comma-separated labels into list
                    if isinstance(value, str):
                        value = [label.strip() for label in value.split(",") if label.strip()]
                    elif not isinstance(value, list):
                        value = [value]
                elif field == "labels" and value is None:
                    value = []  # Default labels to empty list
                result[field] = value

            return result
        except Exception as e:
            if self.verbose:
                print(f"Error parsing issue: {e}")
            return None

    def validate_markdown(self, content: str, doc_type: str) -> Dict[str, Any]:
        """Validate markdown content for a specific document type.

        Args:
            content: Markdown content to validate
            doc_type: Type of document ('workitem' or 'issue')

        Returns:
            Dict with validation results containing 'valid', 'type', 'errors', 'warnings'
        """
        # Check if doc_type is supported
        if doc_type not in ["workitem", "issue"]:
            return {
                "valid": False,
                "type": doc_type,
                "errors": ["Unsupported type"],
                "warnings": [],
            }

        try:
            parsed = self.parse_content(content)
            errors = self.validate_structure(parsed)
            return {
                "valid": len(errors) == 0,
                "type": doc_type,
                "errors": errors,
                "warnings": [],  # No warnings implemented yet
            }
        except Exception as e:
            if self.verbose:
                print(f"Error validating markdown: {e}")
            return {"valid": False, "type": doc_type, "errors": [str(e)], "warnings": []}
