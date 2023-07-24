using System.Linq;
using Event_Based_Impl.BatchProcessing;

namespace Event_Based_Impl.Algorithms {
	/// <summary>
	/// Used this with FluentAPI to preprocess the Files provided in the FilesWrapper object.
	/// Can Remove Inconsistend Pairs
	/// And can set Relative Timestamps
	/// </summary>
	public class Preprocessor {
		private Loader.FilesWrapper _wrapper;

		private Preprocessor(Loader.FilesWrapper wrapper) {
			_wrapper = wrapper;
		}

		/// <summary>
		/// Filters Files where there are no NetPackets or no ChannelTraces
		/// </summary>
		/// <returns>A Preprocessor Instace to use with Fluent API.. Use Result to get back the resulting FilesWrapper</returns>
		public Preprocessor RemoveInconsistendPairs() {
			var list = _wrapper.Zipped().Where(file => file.netPackets.Count != 0 && file.channelTrace.Count != 0)
				.ToList();
			_wrapper = new Loader.FilesWrapper(list);
			return this;
		}

		/// <summary>
		/// Sets Relative Timestamps
		/// </summary>
		/// <returns>A Preprocessor Instace to use with Fluent API.. Use Result to get back the resulting FilesWrapper</returns>
		public Preprocessor SetRelativeTimestamps() {
			_wrapper.Zipped().ForEach(file => Processing.SetRelativeTimeStamps(file.netPackets, file.channelTrace));
			return this;
		}

		/// <summary>
		/// Returns the preprocessed FilesWrapper Object
		/// </summary>
		/// <returns></returns>
		public Loader.FilesWrapper Result() {
			return _wrapper;
		}

		/// <summary>
		/// Populates a new Preprocessor Instance with the Provided FilesWrapper
		/// </summary>
		/// <param name="wrapper">the Fileswrapper to Populate the Preprocessor with</param>
		/// <returns>A populated Preprocessor Object</returns>
		public static Preprocessor get(Loader.FilesWrapper wrapper) {
			return new Preprocessor(wrapper);
		}
	}
}