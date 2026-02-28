// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.
//
// Result.cs
//
// Result pattern implementation for handling operation outcomes with error information.

namespace Sdo.Services
{
    /// <summary>
    /// Represents the result of an operation that can succeed or fail.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets a value indicating whether the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the error message if the operation failed.
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// Gets the exit code for the operation.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class for a successful operation.
        /// </summary>
        protected Result()
        {
            IsSuccess = true;
            Error = null;
            ExitCode = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> class for a failed operation.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <param name="exitCode">The exit code.</param>
        protected Result(string error, int exitCode)
        {
            IsSuccess = false;
            Error = error;
            ExitCode = exitCode;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <returns>A successful result.</returns>
        public static Result Success()
        {
            return new Result();
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <param name="exitCode">The exit code.</param>
        /// <returns>A failed result.</returns>
        public static Result Failure(string error, int exitCode = 1)
        {
            return new Result(error, exitCode);
        }
    }

    /// <summary>
    /// Represents the result of an operation that can succeed or fail, with a value.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    public class Result<T> : Result
    {
        /// <summary>
        /// Gets the result value if the operation succeeded.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result{T}"/> class for a successful operation.
        /// </summary>
        /// <param name="value">The result value.</param>
        private Result(T value) : base()
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Result{T}"/> class for a failed operation.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <param name="exitCode">The exit code.</param>
        private Result(string error, int exitCode) : base(error, exitCode)
        {
            Value = default;
        }

        /// <summary>
        /// Creates a successful result with a value.
        /// </summary>
        /// <param name="value">The result value.</param>
        /// <returns>A successful result with a value.</returns>
        public static Result<T> Success(T value)
        {
            return new Result<T>(value);
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <param name="exitCode">The exit code.</param>
        /// <returns>A failed result.</returns>
        public static new Result<T> Failure(string error, int exitCode = 1)
        {
            return new Result<T>(error, exitCode);
        }
    }
}