using System;
using UnityEngine;

namespace UnityEditor
{
	[CanEditMultipleObjects, CustomEditor(typeof(AudioClip))]
	internal class AudioClipInspector : Editor
	{
		private PreviewRenderUtility m_PreviewUtility;

		private static AudioClipInspector m_PlayingInspector;

		private static AudioClip m_PlayingClip;

		private static bool m_bAutoPlay;

		private static bool m_bLoop;

		private Vector2 m_Position = Vector2.zero;

		private Rect m_wantedRect;

		private static GUIStyle s_PreButton;

		private static GUIContent[] s_PlayIcons = new GUIContent[2];

		private static GUIContent[] s_AutoPlayIcons = new GUIContent[2];

		private static GUIContent[] s_LoopIcons = new GUIContent[2];

		private static Texture2D s_DefaultIcon;

		private static bool playing
		{
			get
			{
				return AudioClipInspector.m_PlayingClip != null && AudioUtil.IsClipPlaying(AudioClipInspector.m_PlayingClip);
			}
		}

		public override void OnInspectorGUI()
		{
		}

		private static void Init()
		{
			if (AudioClipInspector.s_PreButton == null)
			{
				AudioClipInspector.s_PreButton = "preButton";
				AudioClipInspector.m_bAutoPlay = EditorPrefs.GetBool("AutoPlayAudio", false);
				AudioClipInspector.s_AutoPlayIcons[0] = EditorGUIUtility.TrIconContent("preAudioAutoPlayOff", "Turn Auto Play on");
				AudioClipInspector.s_AutoPlayIcons[1] = EditorGUIUtility.TrIconContent("preAudioAutoPlayOn", "Turn Auto Play off");
				AudioClipInspector.s_PlayIcons[0] = EditorGUIUtility.TrIconContent("preAudioPlayOff", "Play");
				AudioClipInspector.s_PlayIcons[1] = EditorGUIUtility.TrIconContent("preAudioPlayOn", "Stop");
				AudioClipInspector.s_LoopIcons[0] = EditorGUIUtility.TrIconContent("preAudioLoopOff", "Loop on");
				AudioClipInspector.s_LoopIcons[1] = EditorGUIUtility.TrIconContent("preAudioLoopOn", "Loop off");
				AudioClipInspector.s_DefaultIcon = EditorGUIUtility.LoadIcon("Profiler.Audio");
			}
		}

		public void OnDisable()
		{
			if (AudioClipInspector.m_PlayingInspector == this)
			{
				AudioUtil.StopAllClips();
				AudioClipInspector.m_PlayingClip = null;
			}
			EditorPrefs.SetBool("AutoPlayAudio", AudioClipInspector.m_bAutoPlay);
		}

		public void OnEnable()
		{
			AudioUtil.StopAllClips();
			AudioClipInspector.m_PlayingClip = null;
			AudioClipInspector.m_PlayingInspector = this;
			AudioClipInspector.m_bAutoPlay = EditorPrefs.GetBool("AutoPlayAudio", false);
		}

		public void OnDestroy()
		{
			if (this.m_PreviewUtility != null)
			{
				this.m_PreviewUtility.Cleanup();
				this.m_PreviewUtility = null;
			}
		}

		public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
		{
			AudioClip clip = base.target as AudioClip;
			AssetImporter atPath = AssetImporter.GetAtPath(assetPath);
			AudioImporter audioImporter = atPath as AudioImporter;
			Texture2D result;
			if (audioImporter == null || !ShaderUtil.hardwareSupportsRectRenderTexture)
			{
				result = null;
			}
			else
			{
				if (this.m_PreviewUtility == null)
				{
					this.m_PreviewUtility = new PreviewRenderUtility();
				}
				this.m_PreviewUtility.BeginStaticPreview(new Rect(0f, 0f, (float)width, (float)height));
				this.DoRenderPreview(clip, audioImporter, new Rect(0.05f * (float)width * EditorGUIUtility.pixelsPerPoint, 0.05f * (float)width * EditorGUIUtility.pixelsPerPoint, 1.9f * (float)width * EditorGUIUtility.pixelsPerPoint, 1.9f * (float)height * EditorGUIUtility.pixelsPerPoint), 1f);
				result = this.m_PreviewUtility.EndStaticPreview();
			}
			return result;
		}

		public override bool HasPreviewGUI()
		{
			return base.targets != null;
		}

		public override void OnPreviewSettings()
		{
			if (AudioClipInspector.s_DefaultIcon == null)
			{
				AudioClipInspector.Init();
			}
			AudioClip audioClip = base.target as AudioClip;
			using (new EditorGUI.DisabledScope(AudioUtil.IsMovieAudio(audioClip)))
			{
				bool flag = base.targets.Length > 1;
				using (new EditorGUI.DisabledScope(flag))
				{
					bool flag2 = !flag && AudioClipInspector.m_bAutoPlay;
					bool flag3 = PreviewGUI.CycleButton((!flag2) ? 0 : 1, AudioClipInspector.s_AutoPlayIcons) != 0;
					if (flag2 != flag3)
					{
						AudioClipInspector.m_bAutoPlay = flag3;
						InspectorWindow.RepaintAllInspectors();
					}
					bool flag4 = !flag && AudioClipInspector.m_bLoop;
					bool flag5 = PreviewGUI.CycleButton((!flag4) ? 0 : 1, AudioClipInspector.s_LoopIcons) != 0;
					if (flag4 != flag5)
					{
						AudioClipInspector.m_bLoop = flag5;
						if (AudioClipInspector.playing)
						{
							AudioUtil.LoopClip(audioClip, flag5);
						}
						InspectorWindow.RepaintAllInspectors();
					}
				}
				using (new EditorGUI.DisabledScope(flag && !AudioClipInspector.playing && AudioClipInspector.m_PlayingInspector != this))
				{
					bool flag6 = AudioClipInspector.m_PlayingInspector == this && AudioClipInspector.playing;
					bool flag7 = PreviewGUI.CycleButton((!flag6) ? 0 : 1, AudioClipInspector.s_PlayIcons) != 0;
					if (flag7 != flag6)
					{
						AudioUtil.StopAllClips();
						if (flag7)
						{
							AudioUtil.PlayClip(audioClip, 0, AudioClipInspector.m_bLoop);
							AudioClipInspector.m_PlayingClip = audioClip;
							AudioClipInspector.m_PlayingInspector = this;
						}
					}
				}
			}
		}

		private void DoRenderPreview(AudioClip clip, AudioImporter audioImporter, Rect wantedRect, float scaleFactor)
		{
			scaleFactor *= 0.95f;
			float[] minMaxData = (!(audioImporter == null)) ? AudioUtil.GetMinMaxData(audioImporter) : null;
			int numChannels = clip.channels;
			int numSamples = (minMaxData != null) ? (minMaxData.Length / (2 * numChannels)) : 0;
			float num = wantedRect.height / (float)numChannels;
			int channel;
			for (channel = 0; channel < numChannels; channel++)
			{
				Rect r = new Rect(wantedRect.x, wantedRect.y + num * (float)channel, wantedRect.width, num);
				Color curveColor = new Color(1f, 0.549019635f, 0f, 1f);
				AudioCurveRendering.DrawMinMaxFilledCurve(r, delegate(float x, out Color col, out float minValue, out float maxValue)
				{
					col = curveColor;
					if (numSamples <= 0)
					{
						minValue = 0f;
						maxValue = 0f;
					}
					else
					{
						float f = Mathf.Clamp(x * (float)(numSamples - 2), 0f, (float)(numSamples - 2));
						int num2 = (int)Mathf.Floor(f);
						int num3 = (num2 * numChannels + channel) * 2;
						int num4 = num3 + numChannels * 2;
						minValue = Mathf.Min(minMaxData[num3 + 1], minMaxData[num4 + 1]) * scaleFactor;
						maxValue = Mathf.Max(minMaxData[num3], minMaxData[num4]) * scaleFactor;
						if (minValue > maxValue)
						{
							float num5 = minValue;
							minValue = maxValue;
							maxValue = num5;
						}
					}
				});
			}
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (AudioClipInspector.s_DefaultIcon == null)
			{
				AudioClipInspector.Init();
			}
			AudioClip audioClip = base.target as AudioClip;
			Event current = Event.current;
			if (current.type != EventType.Repaint && current.type != EventType.Layout && current.type != EventType.Used)
			{
				int num = AudioUtil.GetSampleCount(audioClip) / (int)r.width;
				EventType type = current.type;
				if (type == EventType.MouseDrag || type == EventType.MouseDown)
				{
					if (r.Contains(current.mousePosition) && !AudioUtil.IsMovieAudio(audioClip))
					{
						if (AudioClipInspector.m_PlayingClip != audioClip || !AudioUtil.IsClipPlaying(audioClip))
						{
							AudioUtil.StopAllClips();
							AudioUtil.PlayClip(audioClip, 0, AudioClipInspector.m_bLoop);
							AudioClipInspector.m_PlayingClip = audioClip;
							AudioClipInspector.m_PlayingInspector = this;
						}
						AudioUtil.SetClipSamplePosition(audioClip, num * (int)current.mousePosition.x);
						current.Use();
					}
				}
			}
			else
			{
				if (Event.current.type == EventType.Repaint)
				{
					background.Draw(r, false, false, false, false);
				}
				int channelCount = AudioUtil.GetChannelCount(audioClip);
				this.m_wantedRect = new Rect(r.x, r.y, r.width, r.height);
				float num2 = this.m_wantedRect.width / audioClip.length;
				if (!AudioUtil.HasPreview(audioClip) && (AudioUtil.IsTrackerFile(audioClip) || AudioUtil.IsMovieAudio(audioClip)))
				{
					float num3 = (r.height <= 150f) ? (r.y + r.height / 2f - 25f) : (r.y + r.height / 2f - 10f);
					if (r.width > 64f)
					{
						if (AudioUtil.IsTrackerFile(audioClip))
						{
							EditorGUI.DropShadowLabel(new Rect(r.x, num3, r.width, 20f), string.Format("Module file with " + AudioUtil.GetMusicChannelCount(audioClip) + " channels.", new object[0]));
						}
						else if (AudioUtil.IsMovieAudio(audioClip))
						{
							if (r.width > 450f)
							{
								EditorGUI.DropShadowLabel(new Rect(r.x, num3, r.width, 20f), "Audio is attached to a movie. To audition the sound, play the movie.");
							}
							else
							{
								EditorGUI.DropShadowLabel(new Rect(r.x, num3, r.width, 20f), "Audio is attached to a movie.");
								EditorGUI.DropShadowLabel(new Rect(r.x, num3 + 10f, r.width, 20f), "To audition the sound, play the movie.");
							}
						}
						else
						{
							EditorGUI.DropShadowLabel(new Rect(r.x, num3, r.width, 20f), "Can not show PCM data for this file");
						}
					}
					if (AudioClipInspector.m_PlayingInspector == this && AudioClipInspector.m_PlayingClip == audioClip)
					{
						float clipPosition = AudioUtil.GetClipPosition(audioClip);
						TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, (int)(clipPosition * 1000f));
						EditorGUI.DropShadowLabel(new Rect(this.m_wantedRect.x, this.m_wantedRect.y, this.m_wantedRect.width, 20f), string.Format("Playing - {0:00}:{1:00}.{2:000}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds));
					}
				}
				else
				{
					PreviewGUI.BeginScrollView(this.m_wantedRect, this.m_Position, this.m_wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");
					if (Event.current.type == EventType.Repaint)
					{
						this.DoRenderPreview(audioClip, AudioUtil.GetImporterFromClip(audioClip), this.m_wantedRect, 1f);
					}
					for (int i = 0; i < channelCount; i++)
					{
						if (channelCount > 1 && r.width > 64f)
						{
							Rect position = new Rect(this.m_wantedRect.x + 5f, this.m_wantedRect.y + this.m_wantedRect.height / (float)channelCount * (float)i, 30f, 20f);
							EditorGUI.DropShadowLabel(position, "ch " + (i + 1).ToString());
						}
					}
					if (AudioClipInspector.m_PlayingInspector == this && AudioClipInspector.m_PlayingClip == audioClip)
					{
						float clipPosition2 = AudioUtil.GetClipPosition(audioClip);
						TimeSpan timeSpan2 = new TimeSpan(0, 0, 0, 0, (int)(clipPosition2 * 1000f));
						GUI.DrawTexture(new Rect(this.m_wantedRect.x + (float)((int)(num2 * clipPosition2)), this.m_wantedRect.y, 2f, this.m_wantedRect.height), EditorGUIUtility.whiteTexture);
						if (r.width > 64f)
						{
							EditorGUI.DropShadowLabel(new Rect(this.m_wantedRect.x, this.m_wantedRect.y, this.m_wantedRect.width, 20f), string.Format("{0:00}:{1:00}.{2:000}", timeSpan2.Minutes, timeSpan2.Seconds, timeSpan2.Milliseconds));
						}
						else
						{
							EditorGUI.DropShadowLabel(new Rect(this.m_wantedRect.x, this.m_wantedRect.y, this.m_wantedRect.width, 20f), string.Format("{0:00}:{1:00}", timeSpan2.Minutes, timeSpan2.Seconds));
						}
					}
					PreviewGUI.EndScrollView();
				}
				if (AudioClipInspector.m_bAutoPlay && AudioClipInspector.m_PlayingClip != audioClip && AudioClipInspector.m_PlayingInspector == this)
				{
					AudioUtil.StopAllClips();
					AudioUtil.PlayClip(audioClip, 0, AudioClipInspector.m_bLoop);
					AudioClipInspector.m_PlayingClip = audioClip;
					AudioClipInspector.m_PlayingInspector = this;
				}
				if (AudioClipInspector.playing)
				{
					GUIView.current.Repaint();
				}
			}
		}

		public override string GetInfoString()
		{
			AudioClip clip = base.target as AudioClip;
			int channelCount = AudioUtil.GetChannelCount(clip);
			string text = (channelCount != 1) ? ((channelCount != 2) ? ((channelCount - 1).ToString() + ".1") : "Stereo") : "Mono";
			AudioCompressionFormat targetPlatformSoundCompressionFormat = AudioUtil.GetTargetPlatformSoundCompressionFormat(clip);
			AudioCompressionFormat soundCompressionFormat = AudioUtil.GetSoundCompressionFormat(clip);
			string text2 = targetPlatformSoundCompressionFormat.ToString();
			if (targetPlatformSoundCompressionFormat != soundCompressionFormat)
			{
				text2 = text2 + " (" + soundCompressionFormat.ToString() + " in editor)";
			}
			string text3 = text2;
			text2 = string.Concat(new object[]
			{
				text3,
				", ",
				AudioUtil.GetFrequency(clip),
				" Hz, ",
				text,
				", "
			});
			TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, (int)AudioUtil.GetDuration(clip));
			if ((uint)AudioUtil.GetDuration(clip) == 4294967295u)
			{
				text2 += "Unlimited";
			}
			else
			{
				text2 += string.Format("{0:00}:{1:00}.{2:000}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
			}
			return text2;
		}
	}
}
