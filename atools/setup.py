"""
Setup script for SDO (Simple DevOps) package
"""

from setuptools import setup, find_packages

# Read version from package
def get_version():
    """Get version from sdo_package.version module"""
    try:
        from sdo_package.version import __version__
        return __version__
    except ImportError:
        return "1.0.0"

# Read README if it exists
def get_long_description():
    """Get long description from README.md if it exists"""
    try:
        with open("README.md", "r", encoding="utf-8") as f:
            return f.read()
    except FileNotFoundError:
        return "Simple DevOps CLI tool for Azure DevOps work item management"

setup(
    name="sdo",
    version=get_version(),
    description="Simple DevOps CLI tool for Azure DevOps work item management",
    long_description=get_long_description(),
    long_description_content_type="text/markdown",
    author="naz-hage",
    author_email="",
    url="https://github.com/naz-hage/ntools",
    packages=find_packages(),
    include_package_data=True,
    install_requires=[
        "requests>=2.25.0",
        "click>=8.0.0",
    ],
    entry_points={
        "console_scripts": [
            "sdo=sdo_package.cli:cli",
        ],
    },
    classifiers=[
        "Development Status :: 4 - Beta",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
    ],
    python_requires=">=3.8",
    keywords="azure devops cli work-items",
)