import os

def add_nullable_annotations(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        lines = file.readlines()

    with open(file_path, 'w', encoding='utf-8') as file:
        added_annotations = False
        line_was_using = 0
        for line in lines:
            if (not line.strip().startswith('//')) and (not added_annotations):
                file.write('#nullable enable annotations\n')
                added_annotations = True
            if line.startswith('using'):
                line_was_using = 1
            else:
                if line_was_using > 0:
                    if line.isspace():
                        line_was_using += 1
                        if line_was_using == 3:
                            line_was_using = 0
                            continue
                    else:
                        line_was_using = 0
            file.write(line)
            
def process_cs_files(directory='.'):
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                add_nullable_annotations(file_path)

if __name__ == "__main__":
    process_cs_files()
