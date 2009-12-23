using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Terry
{

    class DemoController
    {
        public Shapes.ShapeType shape = Shapes.ShapeType.Ball;
        public int resolution = 5;
        public int value2 = 20;
        public int stateCounter = 0;
        const int framesPerSec = 10;
        protected LinkedList<Result> queue = new LinkedList<Result>();
        public RenderData renderData;
        public float lastCalculatedTime = 0f;
        public float currentFrameTime = 0f;
        bool doFirst = false;
        CubeRenderer renderer = new CubeRenderer();
        public int queued = 0;
        protected int workerThreads = Environment.ProcessorCount;
        public Flags flags = new Flags();
        protected System.Collections.Generic.Queue<float> FrameTimes = new Queue<float>();

        protected class Request
        {
            public float time;
            public int counter;
            public int resolution;
        }

        protected class Result
        {
            public float time;
            //public Sampler sampler;
            public RenderData renderData;
        }

        public DemoController()
        {
            flags.Multithread = true;
        }

        public void OnStateChange()
        {
            stateCounter++;
            lock (queue)
                queue.Clear();
            Interlocked.Exchange(ref queued, 0);
            //renderData = null;
            lastCalculatedTime = 0f;
            currentFrameTime = 0f;
            doFirst = true;
            Debug.WriteLine("State Change------------------------");
        }

        public void OnFrameMove(double appTime)
        {
            PopQueue(appTime);
            //if (renderData == null)
            //{
            //    CalculateFirstFrame(device, appTime);
           // }
            //else
                CalculateQueue((float)appTime);
        }

        public void CalculateFirstFrame(double appTime)
        {
            //jump forward if necessary    
            if (appTime > lastCalculatedTime)
                lastCalculatedTime = (float)appTime + 1f / framesPerSec;

            Request r = new Request();
            r.time = lastCalculatedTime;
            r.counter = stateCounter;
            //ThreadPool.QueueUserWorkItem(CalculateFrame, (object)r);
            CalculateFrame((object)r);
            PopQueue(appTime);
        }

        public void PopQueue(double appTime)
        {
            RenderData frame = null;
            lock (queue)
            {
                while (queue.Count > 0 && appTime > queue.First.Value.time)
                {
                    if (queue.First.Value.time > currentFrameTime)
                    {
                        Debug.WriteLine(string.Format("Mainthread: Popping frame for time {0} at time {1}", queue.First.Value.time, appTime));
                        //renderData2 = renderData;
                        renderData = queue.First.Value.renderData;
                        frame = renderData;
                        currentFrameTime = queue.First.Value.time;
                        queue.RemoveFirst();
                        break;
                    }
                    else
                    {
                        Debug.WriteLine(string.Format("Mainthread: Discarding frame for time {0} at time {1}", queue.First.Value.time, appTime));
                        frame = queue.First.Value.renderData;
                        queue.RemoveFirst();
                    }
                }

                //adjust resolution
                if (frame != null)
                {
                    FrameTimes.Enqueue(frame.calcMilliseconds + frame.renderMilliseconds);
                    if (FrameTimes.Count > 10)
                    {
                        float time = 0;
                        foreach (float f in FrameTimes)
                            time += f;
                        int frames = (int)(1000f / (time / FrameTimes.Count));

                        if (frames > framesPerSec + 2)
                        {
                            resolution += 1;
                            //OnStateChange();
                        }
                        else if (frames < framesPerSec - 2 && resolution > 2)
                        {
                            resolution -= 1;
                            //OnStateChange();
                        }
                        FrameTimes.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Calculate data for frames in background threads
        /// </summary>
        /// <param name="appTime"></param>
        public void CalculateQueue(float appTime)
        {
            //jump forward if necessary    
            if (appTime > lastCalculatedTime)
                lastCalculatedTime = appTime + 1f / framesPerSec;

            //schedule calculation of frames into thread pool
            int added=0;
            int threads = flags.Multithread ? workerThreads : 1;
            while ((doFirst && queued==0) || (Shapes.IsTimeDependent(shape) && queued < threads && added < threads && (lastCalculatedTime-appTime)<5f))
            {
                Request r = new Request();
                r.time = lastCalculatedTime;
                r.counter = stateCounter;
                r.resolution = resolution;
                Debug.WriteLine(string.Format("Mainthread: Queueing frame calc for time {0} at time {1}, queue length={2}", lastCalculatedTime, appTime, queued));
                if (flags.Multithread)
                    ThreadPool.QueueUserWorkItem(CalculateFrame, (object)r);
                else
                    CalculateFrame(r);
                System.Threading.Interlocked.Increment(ref queued);
                added++;
                lastCalculatedTime += 1f / framesPerSec;
                doFirst = false;
            }
        }

        /// <summary>
        /// Calculate a single frame (inside a thread) and push to queue
        /// </summary>
        /// <param name="t"></param>
        void CalculateFrame(object t)
        {
            Request r = (Request)t;
            if (r.counter != stateCounter)  //ignore if state has changed
            {
                return;
            }
            Result q = new Result();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Sampler sampler = new Sampler(shape);
            sampler.Resolution = r.resolution;
            sampler.OnTimerTick(r.time);
            sampler.Calculate(flags);
            long ct = watch.ElapsedMilliseconds;
            q.renderData = renderer.Render(sampler, flags);
            q.renderData.calcMilliseconds = ct;
            q.renderData.renderMilliseconds = watch.ElapsedMilliseconds-ct;
            q.renderData.description = sampler.Description;
            q.time = r.time;

            if (r.counter == stateCounter)  //ignore if state has changed
            {
                lock (queue)
                {
                    //insert sorted
                    LinkedListNode<Result> node = queue.Last;
                    while (node != null && node.Value.time > q.time)
                        node = node.Previous;
                    if (node == null)
                        queue.AddFirst(q);
                    else
                        queue.AddAfter(node, q);
                    System.Threading.Interlocked.Decrement(ref queued);
                    Debug.WriteLine(string.Format("Completed frame calc for time {0}, calctime={1}, rendertime={2}", r.time, q.renderData.calcMilliseconds, q.renderData.renderMilliseconds));
                }
            }

            //exits thread
        }


        internal bool OnKeyPress(char p)
        {
            return flags.SetFlags(p);
        }
    }


}
