using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fireteams.Common
{
    /// <summary>
    /// Circular buffer.
    /// 
    /// When writting to a full buffer:
    /// PushBack -> removes this[0] / Front()
    /// PushFront -> removes this[Count-1] / Back()
    /// 
    /// this implementation is inspired by
    /// http://www.boost.org/doc/libs/1_53_0/libs/circular_buffer/doc/circular_buffer.html
    /// because I liked their interface.
    /// </summary>
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private T[] _buffer;

        /// <summary>
        /// The _start. Index of the first element in buffer.
        /// </summary>
        private int _start;

        /// <summary>
        /// The _end. Index after the last element in the buffer.
        /// </summary>
        private int _end;

        /// <summary>
        /// Maximum capacity of the buffer. Elements pushed into the buffer after
        /// maximum capacity is reached (IsFull = true), will remove an element.
        /// </summary>
        public int Capacity
        {
            get { return _buffer.Length; }
        }

        public bool IsFull
        {
            get { return Count == Capacity; }
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        /// <summary>
        /// Current buffer size (the number of elements that the buffer has).
        /// </summary>
        public int Count { get; private set; }

        public CircularBuffer(int capacity) : this(capacity, new T[] { })
        {
            //
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
        /// 
        /// </summary>
        /// <param name='capacity'>
        /// Buffer capacity. Must be positive.
        /// </param>
        /// <param name='items'>
        /// Items to fill buffer with. Items length must be less than capacity.
        /// Sugestion: use Skip(x).Take(y).ToArray() to build this argument from
        /// any enumerable.
        /// </param>
        public CircularBuffer(int capacity, T[] items)
        {
            if( capacity < 1 )
                throw new ArgumentException("Circular buffer cannot have negative or zero capacity.", "capacity");

            if( items == null )
                throw new ArgumentNullException("items");

            if( items.Length > capacity )
                throw new ArgumentException("Too many items to fit circular buffer", "items");

            _buffer = new T[capacity];

            Array.Copy(items, _buffer, items.Length);
            Count = items.Length;

            _start = 0;
            _end = Count == capacity ? 0 : Count;
        }

        /// <summary>
        /// Element at the front of the buffer - this[0].
        /// </summary>
        /// <returns>The value of the element of type T at the front of the buffer.</returns>
        public T Front()
        {
            throwIfEmpty();
            return _buffer[_start];
        }

        /// <summary>
        /// Element at the back of the buffer - this[Count - 1].
        /// </summary>
        /// <returns>The value of the element of type T at the back of the buffer.</returns>
        public T Back()
        {
            throwIfEmpty();
            return _buffer[(_end != 0 ? _end : Count) - 1];
        }

        public T this[int index]
        {
            get
            {
                if( IsEmpty )
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));

                if( index >= Count )
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, Count));

                int actualIndex = internalIndex(index);
                return _buffer[actualIndex];
            }
            set
            {
                if( IsEmpty )
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));

                if( index >= Count )
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, Count));

                int actualIndex = internalIndex(index);
                _buffer[actualIndex] = value;
            }
        }

        /// <summary>
        /// Pushes a new element to the back of the buffer. Back()/this[Count-1]
        /// will now return this element.
        /// 
        /// When the buffer is full, the element at Front()/this[0] will be 
        /// popped to allow for this new element to fit.
        /// </summary>
        /// <param name="item">Item to push to the back of the buffer</param>
        public void PushBack(T item)
        {
            if( IsFull )
            {
                _buffer[_end] = item;
                increment(ref _end);
                _start = _end;
            }
            else
            {
                _buffer[_end] = item;
                increment(ref _end);
                ++Count;
            }
        }

        /// <summary>
        /// Pushes a new element to the front of the buffer. Front()/this[0]
        /// will now return this element.
        /// 
        /// When the buffer is full, the element at Back()/this[Count-1] will be 
        /// popped to allow for this new element to fit.
        /// </summary>
        /// <param name="item">Item to push to the front of the buffer</param>
        public void PushFront(T item)
        {
            if( IsFull )
            {
                decrement(ref _start);
                _end = _start;
                _buffer[_start] = item;
            }
            else
            {
                decrement(ref _start);
                _buffer[_start] = item;
                ++Count;
            }
        }

        /// <summary>
        /// Removes the element at the back of the buffer. Decreassing the 
        /// Buffer size by 1.
        /// </summary>
        public void PopBack()
        {
            throwIfEmpty("Cannot take elements from an empty buffer.");
            decrement(ref _end);
            _buffer[_end] = default(T);
            --Count;
        }

        /// <summary>
        /// Removes the element at the front of the buffer. Decreassing the 
        /// Buffer size by 1.
        /// </summary>
        public void PopFront()
        {
            throwIfEmpty("Cannot take elements from an empty buffer.");
            _buffer[_start] = default(T);
            increment(ref _start);
            --Count;
        }

        /// <summary>
        /// Copies the buffer contents to an array, acording to the logical
        /// contents of the buffer (i.e. independent of the internal 
        /// order/contents)
        /// </summary>
        /// <returns>A new array with a copy of the buffer contents.</returns>
        public T[] ToArray()
        {
            return internalToArray();
        }

        #region IEnumerable implementation
        public IEnumerator<T> GetEnumerator()
        {
            var array = internalToArray();

            for( int i=0; i < Count; i++ )
                yield return array[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        private void throwIfEmpty(string message = "Cannot access an empty buffer.")
        {
            if( IsEmpty )
                throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Increments the provided index variable by one, wrapping
        /// around if necessary.
        /// </summary>
        /// <param name="index"></param>
        private void increment(ref int index)
        {
            if( ++index == Capacity )
                index = 0;
        }

        /// <summary>
        /// Decrements the provided index variable by one, wrapping
        /// around if necessary.
        /// </summary>
        /// <param name="index"></param>
        private void decrement(ref int index)
        {
            if( index == 0 )
                index = Capacity;

            index--;
        }

        /// <summary>
        /// Converts the index in the argument to an index in <code>_buffer</code>
        /// </summary>
        /// <returns>
        /// The transformed index.
        /// </returns>
        /// <param name='index'>
        /// External index.
        /// </param>
        private int internalIndex(int index)
        {
            return _start + (index < (Capacity - _start) ? index : index - Capacity);
        }

        #region Array items easy access.
        // doing ArrayOne and ArrayTwo methods returning ArraySegment<T> as seen here: 
        // http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1957cccdcb0c4ef7d80a34a990065818d
        // http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1f5081a54afbc2dfc1a7fb20329df7d5b
        // should help a lot with the code.

        // The array is composed by at most two non-contiguous segments, 
        // the next two methods allow easy access to those.

        private T[] internalToArray()
        {
            var array = new T[Count];
            int offset = 0;

            var segments = new[]{ arrayOne(), arrayTwo() };
            foreach( var segment in segments )
            {
                Array.Copy(segment.Array, segment.Offset, array, offset, segment.Count);
                offset += segment.Count;
            }

            return array;
        }

        private ArraySegment<T> arrayOne()
        {
            return _start < _end ?
                new ArraySegment<T>(_buffer, _start, _end - _start) :
                new ArraySegment<T>(_buffer, _start, _buffer.Length - _start);
        }

        private ArraySegment<T> arrayTwo()
        {
            return _start < _end ?
                new ArraySegment<T>(_buffer, _end, 0) :
                new ArraySegment<T>(_buffer, 0, _end);
        }
        #endregion
    }

    public static class CirculerBufferExt
    {
        public static TimeSpan Average(this CircularBuffer<TimeSpan> buffer)
        {
            return TimeSpan.FromMinutes( buffer.Select( x => x.TotalMinutes ).Average() );
        }
    }
}
