using System.Diagnostics;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Psns.Common.SystemExtensions.Diagnostics
{
    /// <summary>
    /// A wrapper to allow safer use of a .NET System Process
    /// 
    /// A .NET Process is associated to a kernel process handle that may become invalid
    /// after the process exits. After this happens, only certain Process properties and methods
    /// will function properly; otherwise, they throw Exceptions. There is no way to really know
    /// when the Process is no longer valid. This struct provides a safe interface for some of these
    /// Properties and methods to determine the state of a .NET Process.
    /// </summary>
    public struct ProcessState
    {
        public static ProcessState Empty = new ProcessState();

        /// <summary>
        /// The backing kernel Process
        /// </summary>
        readonly Option<Process> _process;

        /// <summary>
        /// A unique identifier
        /// </summary>
        readonly Option<string> _id;

        /// <summary>
        /// Determines range based on the Process arguments
        /// </summary>
        /// <param name="process"></param>
        /// <param name="id"></param>
        public ProcessState(Process process, string id)
        {
            _process = process;
            _id = id;
        }

        ProcessState(Option<Process> process, string id)
        {
            _process = process;
            _id = id;
        }

        /// <summary>
        /// Get the underlying Process's exit code (if still attached)
        /// </summary>
        /// <returns></returns>
        public int GetExitCode()
        {
            var self = this;

            return _process.Match(
                Some: process => self.IsProcessAttached() ? process.ExitCode : Constants.NO_PROCESS_ATTACHED,
                None: () => 0);
        }

        /// <summary>
        /// The Process's unique identifier
        /// </summary>
        /// <returns></returns>
        public string Id => 
            _id.Match(Some: id => id, None: () => string.Empty);

        /// <summary>
        /// Determines if underlying Process still has a valid kernel handle
        /// </summary>
        /// <returns></returns>
        bool IsProcessAttached() =>
            _process.Match(
                Some: p => Try(() => p.Id).Match(Succ: id => true, Fail: ex => false),
                None: () => false);

        /// <summary>
        /// Safely wait for underlying Process to exit (if attached)
        /// </summary>
        /// <returns></returns>
        public bool WaitForExit()
        {
            var self = this;

            return _process.Match(
                Some: process =>
                    match(self.IsProcessAttached(),
                        with(true, _ =>
                            match(process.HasExited,
                                with(true, __ => false),
                                otherwise<bool, bool>(__ =>
                                {
                                    process.WaitForExit();
                                    process.Dispose();

                                    return true;
                                }))),
                        otherwise<bool, bool>(_ => false)),
                None: () => false);
        }

        public override bool Equals(object obj)
        {
            if(obj is ProcessState)
            {
                var state = (ProcessState)obj;

                return state.Id == Id;
            }

            return false;
        }

        public override int GetHashCode() =>
            Id.GetHashCode();

        public override string ToString() =>
            $@"Id: {Id}, ProcessAttached: {IsProcessAttached()}";

        public static bool operator ==(ProcessState x, ProcessState y) =>
            x.Equals(y);

        public static bool operator !=(ProcessState x, ProcessState y) =>
            !x.Equals(y);
    }
}