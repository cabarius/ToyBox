import os

def add_nullable_annotations(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        lines = file.readlines()

    with open(file_path, 'w', encoding='utf-8') as file:
        for line in lines:
            if line.startswith("#nullable enable annotations"):
                continue
            file.write(line)

def process_cs_files(directory='.'):
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                add_nullable_annotations(file_path)

if __name__ == "__main__":
    process_cs_files()