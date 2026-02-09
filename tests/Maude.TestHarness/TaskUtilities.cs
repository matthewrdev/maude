using System.Runtime.CompilerServices;

namespace Maude.TestHarness;

/// <summary>
/// Extension Utils
/// </summary>
public static class TaskUtilities
{
	/// <summary>
	/// Task extension to add a timeout.
	/// </summary>
	/// <returns>The task with timeout.</returns>
	/// <param name="task">Task.</param>
	/// <param name="timeoutInMilliseconds">Timeout duration in Milliseconds.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	public static async Task<T> WithTimeout<T>(this Task<T> task, int timeoutInMilliseconds)
	{
		var retTask = await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds))
			.ConfigureAwait(false);

#pragma warning disable CS8603 // Possible null reference return.
		return retTask is Task<T> ? task.Result : default;
#pragma warning restore CS8603 // Possible null reference return.
	}

	/// <summary>
	/// Task extension to add a timeout.
	/// </summary>
	/// <returns>The task with timeout.</returns>
	/// <param name="task">Task.</param>
	/// <param name="timeout">Timeout Duration.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout) =>
		WithTimeout(task, (int)timeout.TotalMilliseconds);

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
	/// <summary>
	/// Attempts to await on the task and catches exception
	/// </summary>
	public static async void SafeFireAndForget(this Task task,
		Action<Exception> onException = null,
		bool continueOnCapturedContext = false,
		[CallerMemberName] string context = "",
		[CallerFilePath] string filePath = "")
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
	{
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext);
		}
		catch (Exception ex)
		{
			if (onException != null)
			{
				onException.Invoke(ex);
			}
			else
			{
				var tag = Path.GetFileNameWithoutExtension(filePath);

				Console.WriteLine($"An exception occured while running the task for {context}");
				Console.WriteLine((ex));
			}
		}
	}	

}