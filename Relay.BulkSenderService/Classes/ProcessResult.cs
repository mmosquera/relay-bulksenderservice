using System;
using System.Collections.Generic;

namespace Relay.BulkSenderService.Classes
{
    public class ProcessResult
    {
        private int _processedCount;
        private int _errorsCount;
        public string ErrorFileName { get; set; }
        public List<ProcessError> Errors { get; set; }

        public ProcessResult()
        {
            _processedCount = 0;
            _errorsCount = 0;
            Errors = new List<ProcessError>();
        }

        public void AddProcessed()
        {
            _processedCount++;
        }

        public void AddProcessError(int lineNumber, string message)
        {
            AddError(lineNumber, message, ErrorType.PROCESS);
        }

        public void AddUnexpectedError(int lineNumber)
        {
            AddError(lineNumber, "Unexpected error. Contact support for more information.", ErrorType.UNEXPECTED);
        }

        public void AddDeliveryError(int lineNumber, string message)
        {
            AddError(lineNumber, message, ErrorType.DELIVERY);
        }

        public void AddLoginError()
        {
            AddError(0, "Error to authenticate user", ErrorType.LOGIN);
        }

        public int GetProcessedCount()
        {
            return _processedCount;
        }

        public int GetErrorsCount()
        {
            return _errorsCount;
        }

        private void AddError(int lineNumber, string message, ErrorType type)
        {
            _errorsCount++;

            Errors.Add(new ProcessError()
            {
                LineNumber = lineNumber,
                Message = message,
                Type = type,
                Date = DateTime.UtcNow
            });
        }

        public void AddDownloadError(string message)
        {
            AddError(0, message, ErrorType.DOWNLOAD);
        }

        internal void AddRepeatedError(string message)
        {
            AddError(0, message, ErrorType.REPEATED);
        }
    }

    public class ProcessError
    {
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public ErrorType Type { get; set; }
        public DateTime Date { get; set; }

        public string GetErrorLine()
        {
            string line = $"{Date}: {Message}";

            if (LineNumber != 0)
            {
                line += $" processing line:{LineNumber}";
            }

            return line;
        }
    }

    public enum ErrorType
    {
        DOWNLOAD = 1,
        UNZIP = 2,
        LOGIN = 3,
        PROCESS = 4,
        REPEATED = 5,
        UNEXPECTED = 6,
        DELIVERY = 7
    }
}
