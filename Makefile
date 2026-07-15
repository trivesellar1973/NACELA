.PHONY: validate drawings cad test lint clean all

PYTHON ?= python

validate:
	$(PYTHON) src/validate_geometry.py

drawings:
	$(PYTHON) src/generate_drawings.py

cad: validate
	$(PYTHON) src/nacelle_model.py

test:
	$(PYTHON) -m pytest -q

lint:
	$(PYTHON) -m ruff check src tests

all: validate drawings test cad

clean:
	rm -rf outputs/cad outputs/derived_dimensions.json .pytest_cache .ruff_cache
