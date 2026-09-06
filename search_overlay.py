
import os

def search_files():
    root_dir = r"C:\Users\Kyle\Downloads\Projects"
    results = []
    
    for root, dirs, files in os.walk(root_dir):
        # Skip some common build/dependency directories to keep it fast
        if any(x in root.lower() for x in ['bin', 'obj', 'node_modules', '.git', '.vs']):
            continue
        for file in files:
            file_lower = file.lower()
            if file_lower.endswith(".cs") or "overlay" in file_lower or "notif" in file_lower:
                full_path = os.path.join(root, file)
                results.append(full_path)
                
    output_path = r"C:\Users\Kyle\Downloads\Projects\Heaplit\Data\Instructions\search_results.txt"
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        f.write("# Search Results\n")
        for r in results:
            f.write(f"- {r}\n")
            # If it's a small file and matches overlay/notif, let's write its content or first 100 lines
            if os.path.getsize(r) < 100000: # less than 100KB
                try:
                    with open(r, "r", encoding="utf-8", errors="ignore") as pf:
                        content = pf.read()
                    f.write(f"```\n{content}\n```\n\n")
                except Exception as e:
                    f.write(f"Error reading file: {e}\n\n")

if __name__ == "__main__":
    search_files()
