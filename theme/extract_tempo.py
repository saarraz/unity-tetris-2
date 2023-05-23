import time
from pathlib import Path

import click
from mido.midifiles.midifiles import MidiFile


@click.group()
def cli():
    pass


@cli.command()
@click.argument('file', type=click.Path(exists=True, file_okay=True, readable=True, path_type=Path))
def extract(file: Path):
    midi_file = MidiFile(file)

    times = []
    accumulated_time = 0
    for msg in midi_file:
        if msg.type == 'note_on':
            times.append(accumulated_time)
        accumulated_time += msg.time
    click.echo('\n'.join(f'{t}f,' for t in sorted(list(set(times)))))


@cli.command()
@click.argument('file', type=click.Path(exists=True, file_okay=True, readable=True, path_type=Path))
def play(file: Path):
    midi_file = MidiFile(file)
    for message in midi_file.play():
        click.echo(message)


if __name__ == '__main__':
    cli()
