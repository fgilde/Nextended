using System;
using System.Linq;
using System.Windows.Media.Animation;

namespace Nextended.UI.WPF.Extensions
{
	/// <summary>
	/// Erweiterungen für Storyboards und animations
	/// </summary>
	public static class AnimationExtensions
	{
		/// <summary>
		/// Storyboard rückwärts abspielen
		/// </summary>
		public static void BeginReverse(this Storyboard storyboard)
		{
			PlayReverse(storyboard);
		}

		private static void PlayReverse(Storyboard sb, bool recursiveCall = false, 
			double? count = null)
		{
			TimeSpan end = default(TimeSpan);
			if (sb.Duration.HasTimeSpan)
				end = sb.Duration.TimeSpan;
			else
			{
				TimelineCollection timelineCollection = sb.Children;
				foreach (DoubleAnimationUsingKeyFrames keyFrames in timelineCollection.OfType<DoubleAnimationUsingKeyFrames>().Where(timeline1 => timeline1.KeyFrames.Count > 0))
				{
					foreach (DoubleKeyFrame frame in keyFrames.KeyFrames)
					{
						if (frame.KeyTime.TimeSpan > end)
							end = frame.KeyTime.TimeSpan;
					}
				}
			}

			sb.AutoReverse = true;

			if (!recursiveCall && count == null && !sb.RepeatBehavior.Equals(RepeatBehavior.Forever) && sb.RepeatBehavior.HasCount)
				count = sb.RepeatBehavior.Count;
			sb.RepeatBehavior = new RepeatBehavior(1);
			if (count == null || count.Value > 0)
			{
				EventHandler storyboardReversePlayCompleted = null;
				storyboardReversePlayCompleted = (sender, args) =>
				{
					sb.Completed -= storyboardReversePlayCompleted;
					if (count.HasValue)
						count--;
					PlayReverse(sb, true, count);
				};
				sb.Completed += storyboardReversePlayCompleted;
			}

			sb.Begin();
			sb.Pause();
			sb.Seek(end);
			sb.Resume();
		}
	}
}