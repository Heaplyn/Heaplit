# Heaplit Custom Processor API

Heaplit supports fine-tuned data processing through specialized tags that allow your external programs to differentiate between text, images, and network requests.

## ⚡ Shorthand Tags
- **Text**: `@proc_text{operation, data}`
- **Image**: `@proc_img{operation, file_path}`
- **Request**: `@proc_req{method, url, body}`
- **Generic**: `@proc{input}` (Fallback)

---

## 🛠️ How Heaplit Calls Your Program
When any `@proc_` tag is used, Heaplit executes your binary with **Named Arguments**. Your program should use a CLI library (like `argparse` in Python or `yargs` in Node) to parse these.

**Arguments Passed:**
1. `--mode`: Either `text`, `image`, `request`, or `generic`.
2. `--op`: The specific operation string provided (e.g., `summarize`, `resize`, `POST`).
3. `--data`: The payload or path to process.

---

## 🐍 Python (Argparse) Template
```python
import argparse
import sys

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", required=True)
    parser.add_argument("--op", required=True)
    parser.add_argument("--data", required=True)
    args = parser.parse_args()

    if args.mode == "text":
        if args.op == "reverse":
            print(args.data[::-1])
    elif args.mode == "image":
        print(f"Analyzing image at {args.data} for operation: {args.op}")
    elif args.mode == "request":
        method, url, body = args.data.split('|')
        print(f"Simulating {method} to {url}")

if __name__ == "__main__":
    main()
```

---

## 🔵 C# (Native) Template
```csharp
using System;

class Program {
    static void Main(string[] args) {
        string mode = "", op = "", data = "";
        for(int i=0; i<args.Length; i++) {
            if(args[i] == "--mode") mode = args[++i];
            if(args[i] == "--op") op = args[++i];
            if(args[i] == "--data") data = args[++i];
        }

        switch(mode.ToLower()) {
            case "text":
                Console.WriteLine($"Processed Text [{op}]: {data.ToUpper()}");
                break;
            case "image":
                Console.WriteLine($"Processed Image [{op}]: path={data}");
                break;
        }
    }
}
```

1. Open the **LLM Studio** in Heaplit.
2. Enable **External Data Processor**.
3. Point the **Processor Path** to your script (e.g., `python C:\path\to\my_processor.py` or the path to your compiled `.exe`).
4. Test it by asking Heaplit: *"Run your custom processor on 'Hello World'"*.
