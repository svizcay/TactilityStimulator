using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;   // for Stopwatch
using System.Threading;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace Inria.Tactility.Debugging
{
    [BurstCompile]// used for compiling jobs
    public struct SampleJob : IJob
    {
        private bool running;   // should be false by default. we coulnd't initialize it here

        private float elapsedTimePlayingMS;
        public void Execute()
        {
            running = false;
            elapsedTimePlayingMS = 0f;

            float previousTime = Time.realtimeSinceStartup;

            while (running)
            {
                float currentTime = Time.realtimeSinceStartup;
                float deltaTime = currentTime - previousTime;
                Console.WriteLine("deltaTime=" + deltaTime);

                previousTime = currentTime;
            }
        }
    }

    /**
     * the intention of this utility was to have the spatio-temporal update logic running in a secondary thread.
     * we stopped working in this clase because right now unity is not the bottleneck
     * */
    public class PatternThread : MonoBehaviour
    {
        // JobHandle runningJob;

        [SerializeField]
        [Range(1, 100)]
        private int bufferSize = 60;

        [SerializeField]
        private int mainThreadUpdateFrequency;
        private int[] mainThreadFrequencyBuffer;
        private int mainThreadBufferIndex = 0;

        // [SerializeField]
        private int secondaryThreadUpdateFrequency;
        private int[] secondThreadFrequencyBuffer;
        private int secondThreadBufferIndex = 0;

        Thread secondaryThread;
        private bool secondaryThreadRunning = false;

        static readonly object _object = new object();  

        private void InitializeBuffers()
        {
            mainThreadFrequencyBuffer = new int[bufferSize];
            secondThreadFrequencyBuffer = new int[bufferSize];
        }

        // private void Awake()
        // {
        //     InitializeBuffers();

        //     secondaryThread = new Thread(SecondaryThreadRunningMethod);
        //     secondaryThreadRunning = true;
        //     secondaryThread.Start();
        // }

        // private void OnEnable()
        // {
        //     //if (!runningJob)
        //     //{
        //         runningJob = ScheduleJob();
        //     // }
        // }

        private void Update()
        {
            mainThreadFrequencyBuffer[mainThreadBufferIndex] = (int)(1f / Time.deltaTime);
            mainThreadBufferIndex = (mainThreadBufferIndex + 1) % bufferSize;

            // every certain milliseconds, update value in GUI
            int sum = 0;
            for (int i =0; i < bufferSize; ++i)
            {
                sum += mainThreadFrequencyBuffer[i];
            }
            mainThreadUpdateFrequency= sum / bufferSize;

            // lock(_object)
            // {
            //     print("main thread shared value: " + secondaryThreadUpdateFrequency);
            // }
            
        }

        private void SecondaryThreadRunningMethod()
        {
            Stopwatch sw = Stopwatch.StartNew();
            // float previousTime = StopW
            int processedFrames = 0;
            while(secondaryThreadRunning)
            {
                float secondThreadDeltaTime = sw.ElapsedMilliseconds / 1000.0f; // in seconds
                if (secondThreadDeltaTime > 1)
                {
                    // secondThreadFrequencyBuffer[secondThreadBufferIndex] = processedFrames;
                    // secondThreadBufferIndex = (secondThreadBufferIndex + 1) % bufferSize;

                    secondaryThreadUpdateFrequency = (int)(processedFrames / secondThreadDeltaTime);

                    processedFrames = 0;
                    sw.Restart();
                    UnityEngine.Debug.Log("secondary thread value:" + secondaryThreadUpdateFrequency);
                }
                // print("secondary thread delta time " + secondThreadDeltaTime);

                // int sum = 0;
                // for (int i =0; i < bufferSize; ++i)
                // {
                //     sum += secondThreadFrequencyBuffer[i];
                // }

                // secondaryThreadUpdateFrequency= sum / bufferSize;

                // lock(_object)
                // {
                // }
                processedFrames++;
            }
            UnityEngine.Debug.Log("secondary thread about to finish");
        }


        private void OnDisable()
        {
            // runningJob.Complete();
            // lock(secondaryThreadRunning)
            // {

            // }
            // secondary thread is not setting the lock flag, it's read only. so let's wait for it
            secondaryThreadRunning = false;
            secondaryThread.Join();

        }

        private JobHandle ScheduleJob()
        {
            SampleJob job = new SampleJob();
            return job.Schedule(); // this will start the job. use the handle to call Complete()

        }
    }

}
