using System;
using System.Text;
using CODE.Framework.Core.Properties;
using CODE.Framework.Core.Utilities.Extensions;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// Various helper methods related to exceptions
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Analyzes exception information and returns HTML with details about the exception.
        /// </summary>
        /// <param name="exception">Exception object</param>
        /// <returns>Exception HTML</returns>
        public static string GetExceptionHtml(Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append(@"<table><tr><td><font face=""Verdana"" size=""-1"">" + Resources.ExceptionStack + "</br>");
            var errorCount = -1;
            while (exception != null)
            {
                errorCount++;
                if (errorCount > 0) sb.Append("<br>");
                // + "icon"
                sb.Append("<b><span onclick=\"javascript:ShowError('error" + StringHelper.ToString(errorCount) + "','errorTable" + StringHelper.ToString(errorCount) + "');\" name=\"error" + StringHelper.ToString(errorCount) + "\" id=\"error" + StringHelper.ToString(errorCount) + "\">+</span>");
                // Error message
                sb.Append(@"&nbsp");
                sb.Append(exception.Message + "</b>");

                // Error detail
                sb.Append("<table width = \"100%\" id=\"errorTable" + StringHelper.ToString(errorCount) + "\" style=\"display:none\"><tr><td width=\"25\"> </td><td valign=\"top\" bgcolor=\"#ffffcc\"><font face=\"Tahoma\" size=\"-1\" color=\"maroon\"><b>");
                // Exception attributes
                sb.Append(Resources.ExceptionAttributes + "<br><table>");
                // Header
                sb.Append("<tr><td> </td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">" + Resources.ExceptionInformation + "</font>");
                sb.Append("</td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">" + Resources.ExceptionDetail + "</font>");
                sb.Append("</td></tr>");
                // Message
                sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">" + Resources.Message + "");
                sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                sb.Append(exception.Message);
                sb.Append("</td></tr>");
                // Exception type
                sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">" + Resources.ExceptionType);
                sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                sb.Append(StringHelper.ToString(exception.GetType()));
                sb.Append("</td></tr>");
                // Source
                sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">" + Resources.Source);
                sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                sb.Append(exception.Source);
                sb.Append("</td></tr>");
                if (exception.TargetSite != null)
                {
                    // Thrown by code in method
                    sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">" + Resources.ThrownByMethod);
                    sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                    sb.Append(exception.TargetSite.Name);
                    sb.Append("</td></tr>");
                    // Thrown by code in method
                    sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">" + Resources.ThrownByClass);
                    sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                    if (exception.TargetSite.DeclaringType != null) sb.Append(exception.TargetSite.DeclaringType.Name);
                    sb.Append("</td></tr>");
                    sb.Append("</table>");
                }

                // Stack Trace
                sb.Append(Resources.StackTrace + "<br><table>");
                // Header
                sb.Append("<tr><td> </td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">" + Resources.LineNumber + "</font>");
                sb.Append("</td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">" + Resources.Method + "</font>");
                sb.Append("</td><td style=\"BORDER-BOTTOM: black 1px solid\"><font face=\"Tahoma\" size=\"-1\"><font color=\"Navy\">" + Resources.SourceFile + "</font>");
                sb.Append("</td></tr>");
                // Actual stack trace
                var stackLines = exception.StackTrace.Split('\r');
                foreach (var stackLine in stackLines)
                    if (stackLine.IndexOf(" in ", StringComparison.Ordinal) > -1)
                    {
                        // We have detailed info
                        // We only have generic info
                        var detail = stackLine.Trim().Trim();
                        detail = detail.Replace("at ", string.Empty);
                        var at = detail.IndexOf(" in ", StringComparison.Ordinal);
                        var file = detail.Substring(at + 4);
                        detail = detail.Substring(0, at);
                        at = file.IndexOf(":line", StringComparison.Ordinal);
                        var sLine = file.Substring(at + 6);
                        file = file.Substring(0, at);
                        sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\">");
                        sb.Append(sLine);
                        sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                        sb.Append(detail);
                        sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\">");
                        sb.Append(file);
                        sb.Append("</td></tr>");
                    }
                    else
                    {
                        // We only have generic info
                        var detail = stackLine.Trim().Trim();
                        detail = detail.Replace("at ", string.Empty);
                        sb.Append("<tr><td> </td><td><font face=\"Tahoma\" size=\"-1\" color=\"darkgray\">" + Resources.NotApplicable);
                        sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\" color=\"darkgray\">");
                        sb.Append(detail);
                        sb.Append("</td><td><font face=\"Tahoma\" size=\"-1\" color=\"darkgray\">");
                        sb.Append("</td></tr>");
                    }
                sb.Append("</table>");

                // Closing the table
                sb.Append("</td></tr></table>");

                // Next exception
                exception = exception.InnerException;
            }
            sb.Append(@"</font></td></tr></table>");
            // Script needed to expand and collapse
            sb.Append("\r\n<script language=\"JavaScript\">\r\n");
            sb.Append("function ShowError(id,idTable)\r\n{\r\n");
            sb.Append("   obj = document.getElementById(idTable);\r\n");
            sb.Append("   obj2 = document.getElementById(id);\r\n");
            sb.Append("   if (obj.style.display == \"none\")\r\n   {\r\n");
            sb.Append("       obj2.innerHTML = \"-\";\r\n");
            sb.Append("       obj.style.display = \"\";\r\n   }\r\n");
            sb.Append("   else\r\n   {\r\n");
            sb.Append("       obj2.innerHTML = \"+\";\r\n");
            sb.Append("       obj.style.display = \"none\";\r\n   }\r\n");
            sb.Append("}\r\n");
            sb.Append("</script>\r\n");
            return sb.ToString();
        }

        /// <summary>
        /// Analyzes exception information and returns it as a plain text string
        /// </summary>
        /// <param name="exception">Exception object</param>
        /// <returns>string</returns>
        public static string GetExceptionText(Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append(Resources.ExceptionStack + "\r\n\r\n");
            var errorCount = -1;
            while (exception != null)
            {
                errorCount++;
                if (errorCount > 0) sb.Append("\r\n");
                sb.Append(exception.Message);

                // Error detail
                // Exception attributes
                sb.Append("  " + Resources.ExceptionAttributes + "\r\n");
                // Message
                sb.Append("    " + Resources.Message + " " + StringHelper.ToStringSafe(exception.Message) + "\r\n");
                // Exception type
                sb.Append("    " + Resources.ExceptionType + " " + StringHelper.ToStringSafe(exception.GetType()) + "\r\n");
                // Source
                sb.Append("    " + Resources.Source + " " + StringHelper.ToStringSafe(exception.Source) + "\r\n");
                if (exception.TargetSite != null)
                {
                    // Thrown by code in method
                    sb.Append("    " + Resources.ThrownByMethod + " " + StringHelper.ToStringSafe(exception.TargetSite.Name) + "\r\n");
                    if (exception.TargetSite.DeclaringType != null)
                        // Thrown by code in method
                        sb.Append("    " + Resources.ThrownByClass + " " + StringHelper.ToStringSafe(exception.TargetSite.DeclaringType.Name) + "\r\n");
                }

                // Stack Trace
                sb.Append("  " + Resources.StackTrace + "\r\n");
                // Actual stack trace
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    var stackLines = exception.StackTrace.Split('\r');
                    foreach (var stackLine in stackLines)
                        if (!string.IsNullOrEmpty(stackLine))
                            if (stackLine.IndexOf(" in ", StringComparison.Ordinal) > -1)
                            {
                                // We have detailed info
                                // We only have generic info
                                var detail = stackLine.Trim().Trim();
                                detail = detail.Replace("at ", string.Empty);
                                var at = detail.IndexOf(" in ", StringComparison.Ordinal);
                                var file = detail.Substring(at + 4);
                                detail = detail.Substring(0, at);
                                at = file.IndexOf(":line", StringComparison.Ordinal);
                                var lineNumber = file.Substring(at + 6);
                                file = file.Substring(0, at);
                                sb.Append("    " + Resources.LineNumber + ": " + lineNumber + " -- ");
                                sb.Append(Resources.Method + ": " + detail + " -- ");
                                sb.Append(Resources.SourceFile + ": " + file + "\r\n");
                            }
                            else
                            {
                                // We only have generic info
                                var detail = stackLine.Trim().Trim();
                                detail = detail.Replace("at ", string.Empty);
                                sb.Append("    " + Resources.Method + ": " + detail);
                            }
                }

                // Next exception
                exception = exception.InnerException;
            }
            return sb.ToString();
        }
    }
}