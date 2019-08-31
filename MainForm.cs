// Accord.NET Sample Applications
// http://accord-framework.net
//
// Copyright © 2009-2017, César Souza
// All rights reserved. 3-BSD License:
//
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//
//      * Redistributions of source code must retain the above copyright
//        notice, this list of conditions and the following disclaimer.
//
//      * Redistributions in binary form must reproduce the above copyright
//        notice, this list of conditions and the following disclaimer in the
//        documentation and/or other materials provided with the distribution.
//
//      * Neither the name of the Accord.NET Framework authors nor the
//        names of its contributors may be used to endorse or promote products
//        derived from this software without specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//  DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
//  DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//  LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//  ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Audio.Filters;

namespace SampleApp
{
    /// <summary>
    ///   Audio recorder sample application.
    ///   录音机示例应用程序。
    /// </summary>
    /// 
    public partial class MainForm : Form
    {
        #region 属性
        /// <summary>
        /// 创建其支持存储区为内存的流。
        /// </summary>
        private MemoryStream stream { get; set; }
        /// <summary>
        /// 音频源
        /// </summary>
        private IAudioSource source { get; set; }
        /// <summary>
        /// 音频输出接口
        /// </summary>
        private IAudioOutput output { get; set; }
        /// <summary>
        /// 波形音频文件编码器。
        /// </summary>
        private WaveEncoder encoder { get; set; }
        /// <summary>
        /// 波形音频文件解码器。
        /// </summary>
        private WaveDecoder decoder { get; set; }
        /// <summary>
        /// 采样点浮点数数组
        /// </summary>
        private float[] Current { set; get; }
        /// <summary>
        /// 信号中的帧数
        /// </summary>
        private int Frames { get; set; }
        /// <summary>
        /// 获取此信号中的采样总数。
        /// </summary>
        private int Samples { get; set; }
        /// <summary>
        /// 播放持续时间（毫秒）
        /// </summary>
        private TimeSpan Duration { set; get; }
        #endregion

        #region 主窗体初始化事件
        /// <summary>
        /// 主窗体初始化事件
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Configure the wavechart 配置波形图
            chart.SimpleMode = true;
            chart.AddWaveform("wave", Color.Green, 1, false);

            updateButtons();
        }
        #endregion

        #region 按钮事件：开始从声卡录制音频
        /// <summary>
        ///   Starts recording audio from the sound card
        ///   开始从声卡录制音频
        /// </summary>
        /// 
        private void BtnRecord_Click(object sender, EventArgs e)
        {
            // Create capture device
            //[1]创建本地音频捕获设备
            #region 本地音频捕获设备
            /*
             * 本地音频捕获设备（即麦克风）的音频源。
             * 备考：此accord.audio.iaudiosource捕获从本地音频获取的音频数据
             * 捕获设备，如麦克风。使用DirectSound捕获音频
             * 通过slimdx。//有关如何列出捕获设备的说明，
             * 请参见accord.directsound.audioDeviceCollection//文档页。
             */
            #endregion
            source = new AudioCaptureDevice()
            {
                //获取或设置所需的帧大小。
                //监听22050赫兹
                // Listen on 22050 Hz
                DesiredFrameSize = 4096,
                SampleRate = 22050,

                #region 我们将读取16位PCM（脉冲编码调制）
                //我们将读取16位PCM（脉冲编码调制）
                // We will be reading 16-bit PCM
                //PCM 即脉冲编码调制 (Pulse Code Modulation)。
                //https://baike.baidu.com/item/pcm%E7%BC%96%E7%A0%81/10865033?fr=aladdin
                #endregion

                Format = SampleFormat.Format16Bit
            };

            // Wire up some events
            //注册音频资源事件
            source.NewFrame += source_NewFrame;
            source.AudioSourceError += source_AudioSourceError;

            // Create buffer for wavechart control
            //每帧上要读取的样本量。
            Current = new float[source.DesiredFrameSize];

            // Create stream to store file
            //创建流以存储文件
            stream = new MemoryStream();
            encoder = new WaveEncoder(stream);

            // Start
            source.Start();
            updateButtons();
        }

        #endregion

        #region 播放录制的音频流
        /// <summary>
        ///   Plays the recorded audio stream.
        ///   播放录制的音频流。
        /// </summary>
        /// 
        private void BtnPlay_Click(object sender, EventArgs e)
        {
            // First, we rewind the stream
            //【1】首先，我们拨回初始值
            stream.Seek(0, SeekOrigin.Begin);

            // Then we create a decoder for it
            //【2】然后我们为它创建一个解码器
            decoder = new WaveDecoder(stream);
            
            /*Configure the track bar so the cursorcan show the proper current position 
             *【3】 配置跟踪条，使光标可以显示正确的当前位置
            */
            if (trackBar1.Value < decoder.Frames)
                decoder.Seek(trackBar1.Value);
            trackBar1.Maximum = decoder.Samples;

            // Here we can create the output audio device that will be playing the recording
            //【4】在这里我们可以创建将播放录音的输出音频设备
            output = new AudioOutputDevice(this.Handle, decoder.SampleRate, decoder.Channels);

            // 【5】Wire up some events 注册一些事件
            //【5.1】指示帧块已开始执行事件。
            output.FramePlayingStarted += output_FramePlayingStarted;
            output.NewFrameRequested += output_NewFrameRequested;
            output.Stopped += output_PlayingFinished;

            // Start playing!
            output.Play();

            updateButtons();
        }
        #endregion

        #region 停止录制或播放流
        /// <summary>
        ///   Stops recording or playing a stream.
        ///   停止录制或播放流。
        /// </summary>
        /// 
        private void btnStop_Click(object sender, EventArgs e)
        {
            // Stops both cases
            // 停止播放流和音频输出设备
            if (source != null)
            {
                // If we were recording
                source.SignalToStop();
                source.WaitForStop();
            }
            if (output != null)
            {
                // If we were playing
                output.SignalToStop();
                output.WaitForStop();
            }

            updateButtons();

            // Also zero out the buffers and screen
            //同时清空缓冲区和屏幕
            Array.Clear(Current, 0, Current.Length);
            UpdateWaveform(Current, Current.Length);
        }

        #endregion

        #region 当音频源出现错误时，将调用此回调。它可以用来路由异常，这样它们就不会影响音频处理管道。
        /// <summary>
        ///   This callback will be called when there is some error with the audio 
        ///   source. It can be used to route exceptions so they don't compromise 
        ///   the audio processing pipeline.
        ///   当音频源出现错误时，将调用此回调。它可以用来路由异常，这样它们就不会影响音频处理管道。
        /// </summary>
        /// 
        private void source_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }
        #endregion

        #region 每当有新的输入音频帧要处理时，将调用此方法。

        /// <summary>
        ///   This method will be called whenever there is a new input audio frame 
        ///   to be processed. This would be the case for samples arriving at the 
        ///   computer's microphone
        ///   每当有新的输入音频帧要处理时，将调用此方法。
        ///   当采样到达计算机的麦克风时，就会出现这种情况。
        /// </summary>
        /// 
        private void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Save current frame
            eventArgs.Signal.CopyTo(Current);

            // Update waveform
            UpdateWaveform(Current, eventArgs.Signal.Length);

            // Save to memory
            encoder.Encode(eventArgs.Signal);

            // Update counters 更新计数器
            //获取信号持续时间（毫秒）。
            Duration += eventArgs.Signal.Duration;
            //获取此信号中的采样总数。
            Samples += eventArgs.Signal.Samples;
            //获取此信号每个通道中的采样数，称为信号中的帧数。
            Frames += eventArgs.Signal.Length;
        }
        #endregion

        #region 一旦计算机扬声器中的音频开始播放，就会触发此事件。
        /// <summary>
        ///   This event will be triggered as soon as the audio starts playing in the 
        ///   computer speakers. It can be used to update the UI and to notify that soon
        ///   we will be requesting additional frames.
        ///   一旦计算机扬声器中的音频开始播放，就会触发此事件。它可以用来更新用户界面，并通知我们很快将请求额外的帧。
        /// </summary>
        /// 
        private void output_FramePlayingStarted(object sender, PlayFrameEventArgs e)
        {
            UpdateTrackbar(e.FrameIndex);

            if (e.FrameIndex + e.Count < decoder.Frames)
            {
                int previous = decoder.Position;
                decoder.Seek(e.FrameIndex);

                Signal s = decoder.Decode(e.Count);
                decoder.Seek(previous);

                UpdateWaveform(s.ToFloat(), s.Length);
            }
        }
        #endregion

        #region 此事件将在输出设备完成时触发 播放音频流。同样，我们可以使用它来更新用户界面。
        /// <summary>
        /// 此事件将在输出设备完成时触发播放音频流。同样，我们可以使用它来更新用户界面。
        ///   This event will be triggered when the output device finishes
        ///   playing the audio stream. Again we can use it to update the UI.
        /// </summary>
        /// 
        private void output_PlayingFinished(object sender, EventArgs e)
        {
            updateButtons();

            Array.Clear(Current, 0, Current.Length);
            UpdateWaveform(Current, Current.Length);
        }
        #endregion

        #region 当声卡需要播放更多采样时，会触发此事件。
        /// <summary>
        ///   This event is triggered when the sound card needs more samples to be
        ///   played. When this happens, we have to feed it additional frames so it
        ///   can continue playing.
        ///   当声卡需要播放更多样本时，会触发此事件。当这种情况发生时，我们必须给它额外的帧，这样它才能继续播放。
        /// </summary>
        /// 
        private void output_NewFrameRequested(object sender, NewFrameRequestedEventArgs e)
        {
            // This is the next frame index
            e.FrameIndex = decoder.Position;

            // Attempt to decode the requested number of frames from the stream
            Signal signal = decoder.Decode(e.Frames);

            if (signal == null)
            {
                // We could not get the requested number of frames. When
                // this happens, this is an indication we need to stop.
                e.Stop = true;
                return;
            }

            // Inform the number of frames
            // actually read from source
            e.Frames = signal.Length;

            // Copy the signal to the buffer
            signal.CopyTo(e.Buffer);
        }

        #endregion



        #region 更新的声音显示
        /// <summary>
        ///   Updates the audio display in the wave chart
        ///   更新的声音显示
        /// </summary>
        /// 
        private void UpdateWaveform(float[] samples, int length)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    chart.UpdateWaveform("wave", samples, length);
                }));
            }
            else
            {
                chart.UpdateWaveform("wave", Current, length);
            }
        }
        #endregion


        #region 更新轨迹栏的当前位置
        /// <summary>
        ///   Updates the current position at the trackbar.
        ///   更新轨迹栏的当前位置。
        /// </summary>
        /// 
        private void UpdateTrackbar(int value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
                }));
            }
            else
            {
                trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
            }
        }
        #endregion


        #region 刷新按钮事件
        /// <summary>
        /// 刷新按钮事件
        /// </summary>
        private void updateButtons()
        {
            //是否需要invoke
            if (InvokeRequired)
            {
                BeginInvoke(new Action(updateButtons));
                return;
            }
            //音频资源播放时
            if (source != null && source.IsRunning)
            {
                btnBwd.Enabled = false;
                btnFwd.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = false;
            }
            //输出设备以获取并正在运行时
            else if (output != null && output.IsRunning)
            {
                btnBwd.Enabled = false;
                btnFwd.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = true;
            }
            else
            {
                btnBwd.Enabled = false;
                btnFwd.Enabled = false;
                //流存在的话
                btnPlay.Enabled = stream != null;
                btnStop.Enabled = false;
                btnRecord.Enabled = true;
                //解码器流存在的话
                trackBar1.Enabled = decoder != null;

                trackBar1.Value = 0;
            }
        }
        #endregion

        #region 窗体关闭事件
        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            if (source != null) source.SignalToStop();
            if (output != null) output.SignalToStop();
        }
        #endregion

        #region 对话框保存事件
        /// <summary>
        /// 对话框保存事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Stream fileStream = saveFileDialog1.OpenFile();
            stream.WriteTo(fileStream);
            fileStream.Close();
        }
        #endregion

        #region 保存对话框显示事件
        /// <summary>
        /// 保存对话框显示事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog(this);
        }
        #endregion

        #region 播放总时长显示事件
        /// <summary>
        /// 播放总时长显示事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            lbLength.Text = String.Format("Length: {0:00.00} sec.", Duration.Seconds);
        }
        #endregion

        #region 关于对话框显示事件
        /// <summary>
        /// 关于对话框显示事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog(this);
        }

        #endregion

        #region MyRegion
        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnIncreaseVolume_Click(object sender, EventArgs e)
        {
            AdjustVolume(1.25f);
        }

        private void BtnDecreaseVolume_Click(object sender, EventArgs e)
        {
            AdjustVolume(0.75f);
        }
        #endregion


        #region 方法：调整音量
        /// <summary>
        /// 方法：调整音量
        /// </summary>
        /// <param name="value"></param>
        private void AdjustVolume(float value)
        {
            // First, we rewind the stream
            stream.Seek(0, SeekOrigin.Begin);

            // Then we create a decoder for it
            decoder = new WaveDecoder(stream);

            var signal = decoder.Decode();

            // We apply the volume filter
            var volume = new VolumeFilter(value);
            volume.ApplyInPlace(signal);

            // Then we store it again
            stream.Seek(0, SeekOrigin.Begin);
            encoder = new WaveEncoder(stream);
            encoder.Encode(signal);
        }
        #endregion


    }
}