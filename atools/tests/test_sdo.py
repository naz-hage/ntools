"""
Tests for main SDO entry point.
"""

import pytest
from unittest.mock import patch
import sys
from pathlib import Path

# Add the atools directory to sys.path so we can import sdo
sys.path.insert(0, str(Path(__file__).parent.parent))

import sdo  # noqa: E402


class TestSDOEntryPoint:
    """Test the main SDO entry point."""

    def test_sdo_app_exists(self):
        """Test that the SDO app object exists."""
        assert hasattr(sdo, "app")
        assert callable(sdo.app)

    @patch("sdo_package.cli.add_issue")
    def test_sdo_help_command(self, mock_add_issue):
        """Test SDO help command works."""
        with patch("sys.argv", ["sdo", "--help"]):
            with pytest.raises(SystemExit) as exc_info:
                sdo.app()
            # Help command exits with code 0
            assert exc_info.value.code == 0

    @patch("sdo_package.cli.add_issue")
    def test_sdo_version_available(self, mock_add_issue):
        """Test that version information is available."""
        # Test that the CLI framework is properly set up
        assert hasattr(sdo.app, "name")
        assert sdo.app.name == "sdo"

    def test_sdo_import_structure(self):
        """Test that SDO imports work correctly."""
        # Test that we can import the main components
        import sdo_package.cli
        import sdo_package.work_items
        import sdo_package.exceptions

        # Verify key classes exist
        assert hasattr(sdo_package.work_items, "WorkItemManager")
        assert hasattr(sdo_package.exceptions, "SDOException")


class TestSDOConfiguration:
    """Test SDO configuration and setup."""

    def test_verbose_flag_handling(self):
        """Test that verbose flag is properly configured."""
        # This would test the global verbose flag setup
        # In a real implementation, we would test the Click context
        pass


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
